from __future__ import absolute_import
from __future__ import division
from __future__ import print_function

import json 
import os 
import cv2 as cv 
import numpy as np 
import threading
import traceback
import inspect
import ctypes
import shutil
import time
import requests

from .lib.config import server_config, ClientConfig
from .lib.Logger import system_log

from .lib.CheckSimilar.check_similar import CheckSimilar
from .lib.Interface.ClassificationFactory import ClassificationFactor
from .lib.Interface.Trainer import Trainer
from .lib.Registration.Reg import Registration, calculate_Hs, registration_extract_patchs_and_rois, registration_extract_single_patch_and_roi


current_path = os.path.dirname(__file__)
# define log file
log_folder = os.path.join(current_path, "log")
if not os.path.exists(log_folder):
    os.makedirs(log_folder)
log_path = os.path.join(log_folder, "server.log")
system_log.set_filepath(log_path)

max_mission = 3

# global variable
mission_count = 0
train_thread_list = []
loop_state = True
train_list = []
process_thread = None 
init_sort_dict = {}    # used to fast init sort
test_model_list = {}


def _async_raise(tid, exctype):
    """raises the exception, performs cleanup if needed"""
    tid = ctypes.c_long(tid)
    if not inspect.isclass(exctype):
        exctype = type(exctype)
    res = ctypes.pythonapi.PyThreadState_SetAsyncExc(tid, ctypes.py_object(exctype))
    if res == 0:
        raise ValueError("invalid thread id")
    elif res != 1:
        # """if it returns a number greater than one, you're in trouble,
        # and you should call it again with exc=NULL to revert the effect"""
        ctypes.pythonapi.PyThreadState_SetAsyncExc(tid, None)
        raise SystemError("PyThreadState_SetAsyncExc failed")

def stop_thread(thread):
    _async_raise(thread.ident, SystemExit)

def train_SVM(function_name, model_root, imgs_list, select_result, is_hog, is_rgb_hist, is_lbp, is_otsu):
    global mission_count, train_thread_list

    trainer = Trainer(function_name, model_root)
    result = trainer.train_SVM(imgs_list, select_result, is_hog, is_rgb_hist, is_lbp, is_otsu)

    if result == 0:
        data_json = {"function_name": function_name, "train_state": True}
    else:
        data_json = {"function_name": function_name, "train_state": False}

    requests.post(server_url, data_json)

    mission_count -= 1
    system_log.WriteLine(f"process finish: {function_name}, mission count: {mission_count}/{max_mission}.")
    # remove the thread from train_thread_list
    for idx,th in enumerate(train_thread_list):
        if th["function_name"] == function_name:
            train_thread_list.pop(idx)
            break
    return True

def process():
    global mission_count, loop_state, train_list, train_thread_list
    system_log.WriteLine(f"process start listening...")

    while(loop_state):
        while(mission_count < max_mission):
            if not len(train_list) == 0:
                train_json = train_list.pop(0)
                function_type = train_json["function_type"]
                function_name = train_json["function_name"]
                if function_type == "SVM":
                    t = threading.Thread(target=train_SVM, args=(function_name, train_json["model_root"], train_json["param"]["imgs_list"], train_json["param"]["select_result"], train_json["param"]["is_hog"], train_json["param"]["is_rgb_hist"], train_json["param"]["is_lbp"], train_json["param"]["is_otsu"],))
                    t.start()
                    mission_count += 1
                    thread_json = {"function_name": function_name, "thread": t}
                    train_thread_list.append(thread_json)
                    system_log.WriteLine(f"start process: {function_name}, mission count: {mission_count}/{max_mission}.")
            if not loop_state:  # if stop the thread, this must be break the loop
                break
            time.sleep(2)

        time.sleep(2)

###################################################################################
#   Interface                                                                     #
###################################################################################

def start(url):
    global process_thread, loop_state, server_url, mission_count, max_mission
    local_config_file_path = os.path.join(current_path, "lib/server.json")
    with open(local_config_file_path, "r") as f:
        server_config.update_config(json.load(f))

    server_url = url
    max_mission = server_config.System["max_mission"]
    mission_count = 0
    loop_state = True
    process_thread = threading.Thread(target=process)
    process_thread.start()
    system_log.WriteLine(f"start thread, url: {url}")
    return True

def stop():
    global loop_state, process_thread, train_list, train_thread_list, mission_count
    loop_state = False
    stop_thread(process_thread)

    for th in train_thread_list:
        stop_thread(th["thread"])

    mission_count = 0
    train_list = []
    system_log.WriteLine(f"stop thread. clear the train list")

def add_SVM_function(function_name, model_root, positive_imgs_list, negative_imgs_list, is_hog=True, is_rgb_hist=False, is_lbp=False, is_otsu=False):
    global train_list

    imgs_list = positive_imgs_list + negative_imgs_list
    label_positive = list(np.ones(len(positive_imgs_list)))
    label_negative = list(np.zeros(len(negative_imgs_list)))
    select_result = label_positive + label_negative

    SVM_param = {
        "imgs_list": imgs_list,
        "select_result": select_result,
        "is_hog": is_hog,
        "is_rgb_hist": is_rgb_hist,
        "is_lbp": is_lbp,
        "is_otsu": is_otsu
    }

    list_json = {
        "function_name": function_name,
        "function_type": "SVM",
        "model_root": model_root,
        "param": SVM_param
    }

    train_list.append(list_json)
    system_log.WriteLine(f"[{function_name}]:   add SVM to train list.")

    return True

def stop_function_training(function_name):
    '''delete a function by function name.'''
    global train_thread_list, train_list, mission_count

    # if the function is in train_list, remove it.
    for idx,DL_json in enumerate(train_list):
        function_name_json = DL_json["function_name"]
        if function_name_json == function_name:
            train_list.pop(idx)
            system_log.WriteLine(f"remove the funciton {function_name} from train list.")
            break

    # if the function is training, stop it.
    for idx, thread_json in enumerate(train_thread_list):
        if thread_json["function_name"] == function_name:
            stop_thread(thread_json["thread"])
            train_thread_list.pop(idx)
            mission_count -= 1
            system_log.WriteLine(f"stop the training mission, mission count: {mission_count}/{max_mission}")
            break

def sort(golden_sample, target_img):
    golden_cv_img = cv.imread(golden_sample)
    target_cv_img = cv.imread(target_img)

    model = CheckSimilar()
    model.Initial_img(golden_cv_img)
    value = model.compare_img(target_cv_img)
    return value

def initial_sort_fast(user_name, golden_sample):
    '''initial inference. call this funciton before use inference'''
    global init_sort_dict

    cv_img = cv.imread(golden_sample)

    model = CheckSimilar()
    model.Initial_img(cv_img)
    init_sort_dict[user_name] = model
    system_log.WriteLine(f"initial inference fast by hand features, user name: {user_name}")

def sort_fast(user_name, target_img):
    '''caculate a image similar.'''
    global init_sort_dict
    model = init_sort_dict[user_name]
    cv_img = cv.imread(target_img)
    value = model.compare_img(cv_img)
    return value

def release_sort_user(user_name):
    '''delete a user from init_sort_dict'''
    global init_sort_dict
    try:
        init_sort_dict.pop(user_name)
        system_log.WriteLine(f"release user success: {user_name}")
    except:
        exstr = traceback.format_exc()
        system_log.WriteLine(f"{exstr}")

def test_add_model(function_name, model_root, golden_sample_path, cfg):
    global test_model_list

    client_cfg = ClientConfig()
    client_cfg.update_config(cfg)

    device = 'gpu' if server_config.System['use_gpu'] == True else 'cpu'
    
    try:
        Classifier = ClassificationFactor(function_name, golden_sample_path, device, client_cfg)
        result, version = Classifier.load_model(model_root)

        if result == -1:
            return False, version
        else:
            test_model_list[function_name] = Classifier 
            return True, version
    
    except:
        exstr = traceback.format_exc()
        system_log.WriteLine(f"{exstr}")
        return False, version

def test_img(function_name, img_path):
    try:
        model = test_model_list[function_name]
        cv_img = cv.imread(img_path)
        system_log.WriteLine(f"load image from {img_path}")
        result = model.predict(cv_img)
        
        if result == 1:
            return True
        else:
            return False 

    except:
        exstr = traceback.format_exc()
        system_log.WriteLine(f"{exstr}")
        return False

def test_release_model(function_name):
    global test_model_list
    
    try:
        test_model_list.pop(function_name)
        system_log.WriteLine(f"release test model success: {function_name}")
    except:
        exstr = traceback.format_exc()
        system_log.WriteLine(f"{exstr}")


def get_reg_roi(target_img, dst_path, roi, H):
    imageId = 1
    _, new_roi = registration_extract_single_patch_and_roi(imageId, target_img, dst_path, H, roi)  
    return new_roi

def get_H(reg_param, golden, target_img):
    golden_img = cv.imread(golden)
    R = Registration(reg_param, golden_img)
    cv_img = cv.imread(target_img)
    H = R.getH(cv_img)

    try:
        H = H.tolist()
    except:
        H = None
        
    return H

def server_multil_calculate_Hs(reg_param,golden_sample,imgs_id,imgs_path,threadCount):
    imgs_id, Hs = calculate_Hs(reg_param, golden_sample, imgs_id, imgs_path, workers=threadCount)
    return imgs_id,Hs

def server_registration_extract_patchs_rois(imgs_id, Hs, imagePaths, dstPaths, roi,threadCount):
    imgs_id, Rois = registration_extract_patchs_and_rois(imgs_id, Hs, imagePaths, dstPaths, roi, workers=threadCount)
    return imgs_id,Rois

def getGROI(H, roi_rect):
    Reg = Registration()
    new_roi = Reg.getGROI(H, roi_rect)
    return new_roi