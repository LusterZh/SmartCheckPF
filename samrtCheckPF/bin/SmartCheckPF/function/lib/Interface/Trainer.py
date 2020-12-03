import os 
import time 
import traceback

from ..SVM.svm_model import SVMModel

from ..Logger import system_log


class Trainer(object):
    def __init__(self, function_name, model_root):
        self.function_name = function_name
        self.model_root = model_root

    def train_SVM(self, imgs_list, select_result, is_hog, is_rgb_hist, is_lbp, is_otsu):
        if not os.path.exists(self.model_root):
            os.makedirs(self.model_root)

        model_path = os.path.join(self.model_root, "model.m")
        # training
        try:
            start = time.time()
            model = SVMModel(self.function_name, is_hog, is_rgb_hist, is_lbp, is_otsu)
            model.train(imgs_list, select_result, model_path)
            end = time.time()
            system_log.WriteLine(f"[{self.function_name}]   training finish, cost time: {(end-start):.8f}sec!")
            return 0
        
        except:
            exstr = traceback.format_exc()
            system_log.WriteLine(f"{exstr}")
            return -1