from __future__ import absolute_import
from __future__ import division
from __future__ import print_function

from .lib.config import client_config
from .lib.Logger import system_log

import requests
import json 
import os
import cv2 as cv 
import numpy as np 
import threading
import traceback
import ctypes
import inspect
import time

from .lib.Interface.ClassificationFactory import ClassificationFactor
from .lib.Interface.GluePathInspection import GluePathInspection
from .lib.Registration.Reg import Registration

current_path = os.path.dirname(__file__)
# define log file
log_folder = os.path.join(current_path, "log")
if not os.path.exists(log_folder):
    os.makedirs(log_folder)
log_path = os.path.join(log_folder, "client.log")
system_log.set_filepath(log_path)

client_url = None

mask_path = os.path.join(current_path, "mask.png")
merge_path = os.path.join(current_path, "merge.jpg")

gluepath_inspection = None
seg_result = {}
image_json_list = []
inference_function_list = []
function_model_list = {}
roi_list = {}
loop_state = True

reg_0 = None
reg_1 = None
gluepath_inspection_use_gpu = False
normal_inspection_use_gpu = False


process_thread = None
seg_thread = None

def ExtractROI(image, image_roi):
    x,y,h,w = image_roi
    img_shape = image.shape
    if len(img_shape) == 3:
        roi = image[y:y+h, x:x+w, :]
    else:
        roi = image[y:y+h, x:x+w]

    return roi

def GluePathPredict(cv_img):
    global seg_result
    seg_result = {}

    try:
        system_log.WriteLine(f"start seg forward.")
        ans, boxes = gluepath_inspection.predict(cv_img, mask_path, merge_path) 
        system_log.WriteLine(f"seg predict done. boxes: {boxes}")

    except:
        exstr = traceback.format_exc()
        system_log.WriteLine(f"{exstr}")
        return False
   
    seg_result["functionID"] = 0
    seg_result["IsFail"] = 1 - int(ans)
    seg_result["rois"] = boxes

    return True

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

def process():
    global image_json_list, seg_result, seg_thread, loop_state
    while(loop_state):
        if not len(image_json_list) == 0:
            try:
                img_json = image_json_list.pop(0)
                img_path = img_json["img_path"]
                system_log.WriteLine(f"pick image from image list. image path is {img_path}")
                seg_thread = None
                rtn = {}
                rtn["state"] = 1
                rtn["imgPath"] = img_path
                rtn["requireSeg"] = 0
                rtn["segResult"] = {}
                rtn["functionResult"] = []
                rtn["H"] = []

                if client_config.System["normal_inspection_registration"] == True:
                    try:
                        H = img_json["H"]
                        rtn["H"] = H.tolist()
                    except:
                        rtn["H"] = []
                        system_log.WriteLine(f"ERROR: H is None!")

                # seg model
                if not gluepath_inspection == None:
                    seg_img = img_json["seg_img"]
                    seg_thread = threading.Thread(target=GluePathPredict, args=(seg_img,))
                    seg_thread.start()
                    rtn["requireSeg"] = 1

                # function model
                for function_name in inference_function_list:
                    if function_name in function_model_list.keys():
                        model = function_model_list[function_name]
                        roi = img_json["rois"][function_name]
                        patch = img_json["patchs"][function_name]
                        result = model.predict(patch)

                        rtn_function_json = {}
                        rtn_function_json["roi"] = {}
                        rtn_function_json["functionId"] = function_name
                        rtn_function_json["IsFail"] = 1 - result

                        rtn_function_json["roi"]["left"] = roi[0]                   # x
                        rtn_function_json["roi"]["top"] = roi[1]                    # y
                        rtn_function_json["roi"]["bottom"] = roi[1] + roi[2]        # h
                        rtn_function_json["roi"]["right"] = roi[0] + roi[3]         # w

                        rtn["functionResult"].append(rtn_function_json)
                    else:
                        system_log.WriteLine(f"function: {function_name} is not found. please add function first.")

                if not seg_thread == None:
                    # wait for seg model inference until it's finished
                    seg_thread.join()
                    rtn["segResult"] = seg_result

            except:
                exstr = traceback.format_exc()
                system_log.WriteLine(f"{exstr}")
                rtn["state"] = 0

            finally:
                post_json = json.dumps(rtn)
                requests.post(client_url, post_json)
                system_log.WriteLine(f"post request, url= {client_url}, json is {json.dumps(rtn)}")

        time.sleep(0.1)



#####################################################
#####                  InterFace                #####
#####################################################
def InitialSegModel(model_root):
    global gluepath_inspection

    try:
        model_path = os.path.join(model_root, "model.pth")
        device = "cuda" if gluepath_inspection_use_gpu else "cpu"
        gluepath_inspection = GluePathInspection("STM", device)
        gluepath_inspection.load_model(model_path)
        system_log.WriteLine(f"Initial Seg Model Done!")
        return True

    except:
        gluepath_inspection = None
        exstr = traceback.format_exc()
        system_log.WriteLine(f"{exstr}")
        return False

def start(client_param_list, reg_param_str_list, golden_sample_list):
    global process_thread, loop_state, reg_0, reg_1, gluepath_inspection_use_gpu, normal_inspection_use_gpu, client_url
    
    system_log.WriteLine(f"client_param_list: {client_param_list[0]}")
    try:
        client_param_json = json.loads(client_param_list[0])   
        if type(client_param_json) == str:
            client_param_json = json.loads(client_param_json)

        if client_param_json["System"]["new_project"] == True:
            system_log.WriteLine(f"new project.")
            return True

        client_config.update_config(client_param_json)
        local_config_file_path = os.path.join(current_path, "lib/client.json")
        with open(local_config_file_path, "r") as f:
            local_param = json.load(f)
            if local_param["System"]["local_config"] == True:
                client_config.update_config(local_param)
                system_log.WriteLine(f"use local config.")     

        if len(reg_param_str_list) > 0 and len(golden_sample_list) > 0:
            golden_sample_0 = cv.imread(golden_sample_list[0])
            reg_param_0 = json.loads(reg_param_str_list[0])
            reg_0 = Registration(reg_param_0, golden_sample_0)

            if len(reg_param_str_list) == 2 and len(golden_sample_list) == 2:
                golden_sample_1 = cv.imread(golden_sample_list[1])
                reg_param_1 = json.loads(reg_param_str_list[1])
                reg_1 = Registration(reg_param_1, golden_sample_1)

        process_thread = threading.Thread(target=process)
        loop_state = True
        gluepath_inspection_use_gpu = client_config.System["gluepath_inspection_use_gpu"]
        normal_inspection_use_gpu = client_config.System["normal_inspection_use_gpu"]
        client_url = client_config.System["client_url"]
        
        process_thread.start()
        system_log.WriteLine(f"start thread.")

        return True
    
    except:
        exstr = traceback.format_exc()
        system_log.WriteLine(f"{exstr}")
        system_log.WriteLine(f"start thread failed.")
        return False

def stop():
    global process_thread, seg_thread, loop_state
    try:
        stop_thread(process_thread)
        process_thread = None
        loop_state = False
        if not seg_thread == None:
            stop_thread(seg_thread)
            seg_thread = None
        system_log.WriteLine(f"stop thread.")
        system_log.close()

        return True
    except:
        exstr = traceback.format_exc()
        system_log.WriteLine(f"{exstr}")
        return False

def add_model(function_name, roi, model_root):
    global function_model_list, roi_list
    try:
        golden_sample_path = None
        device = "cuda" if normal_inspection_use_gpu else "cpu"
        model = ClassificationFactor(function_name, golden_sample_path, device, client_config)
        load_state, version = model.load_model(model_root)

        if load_state == 0:
            function_model_list[function_name] = model
            roi_list[function_name] = roi

            return True, version
        elif load_state == -1:
            return False, version

    except:
        system_log.WriteLine(f"add model {function_name} failed. please checkout the model path and function name.")
        exstr = traceback.format_exc()
        system_log.WriteLine(f"{exstr}")
        return False, version


def release_model(function_name):
    global roi_list, function_model_list

    roi_list.pop(function_name)
    function_model_list.pop(function_name)
    system_log.WriteLine(f"release function {function_name}")

    return True

def inference(img_path, function_list, stat):
    '''stat: 0=zheng 1=fan'''
    global image_json_list, inference_function_list

    try:
        inference_function_list = [str(i) for i in function_list]
        image_json = {}
        image_json["rois"] = {}
        image_json["patchs"] = {}
        image_json["img_path"] = img_path
        cv_img = cv.imread(img_path)
        image_json["cv_img"] = cv_img
        image_json["seg_img"] = cv_img
        image_json["H"] = None
        img_h, img_w, _ = cv_img.shape
        
        if client_config.System["normal_inspection_registration"] == True or client_config.System["gluepath_inspection_registration"] == True:
            if stat == 0:
                reg = reg_0
            else:
                reg = reg_1

            H = reg.getH(cv_img)
            image_json["H"] = H 
            

        for function_name in inference_function_list:
            try:
                roi = roi_list[function_name]

            except:
                system_log.WriteLine(f"function {function_name} has not been added. please add function first.")
                continue

            if client_config.System["normal_inspection_registration"] == True or client_config.System["gluepath_inspection_registration"] == True:
                patch, new_roi = reg.getPatchAndRoi(cv_img, roi, H)
                
            else:
                patch = ExtractROI(cv_img, roi)
                new_roi = roi 
                 
            image_json["patchs"][function_name] = patch
            image_json["rois"][function_name] = new_roi

        if client_config.System["gluepath_inspection_registration"] == True:
            seg_img, _ = reg.getPatchAndRoi(cv_img, [0,0,img_h,img_w], H)
        else:
            seg_img = cv_img

        image_json["seg_img"] = seg_img

        image_json_list.append(image_json)
        system_log.WriteLine(f"add image to list. image path is {img_path}, stat: {stat}, function_list: {function_list}")
        return True
        
    except:
        exstr = traceback.format_exc()
        system_log.WriteLine(f"{exstr}")
        return False

def release_SegModel():
    global gluepath_inspection, seg_thread

    try:
        gluepath_inspection = None
        if not seg_thread == None:
            stop_thread(seg_thread)
            seg_thread = None
        torch.cuda.empty_cache()

        system_log.WriteLine(f"stop segmentation.")
        return True
    
    except:
        exstr = traceback.format_exc()
        system_log.WriteLine(f"{exstr}")
        return False


        



    



