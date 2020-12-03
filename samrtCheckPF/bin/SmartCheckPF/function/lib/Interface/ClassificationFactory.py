import os
import traceback
import torch 
import cv2 as cv
from ..SVM.svm_model import SVMModel
from ..Detection.Detector import Detector

from ..Logger import system_log

detection_type_dict = {
    "one_class_detection": "MobileNet_CenterNet"
}

class ClassificationFactor(object):
    def __init__(self, function_name, golden_sample_path, device='cpu', config=None):
        self.model = None 
        self.function_name = function_name
        self.device = device
        self.golden_sample_path = golden_sample_path
        self.log_root = system_log.get_root()
        self.input_down_ratio = 1
        self.type = "DL"
        self.cfg = config

    def load_model(self, model_root):
        version = ""
        try:
            pth_path = os.path.join(model_root, "model.pth")
            m_path = os.path.join(model_root, "model.m")
            if os.path.exists(pth_path):
                model_path = pth_path
                self.type = "DL"
                
                if self.device == 'cpu':
                    checkpoint = torch.load(model_path, map_location='cpu')
                else:
                    checkpoint = torch.load(model_path)
                
                detection_type = checkpoint['detection_type']
                version = checkpoint['version']
                epoch = checkpoint['epoch']
                state_dict = checkpoint['state_dict']
                num_class = checkpoint['num_class']
                if "input_down_ratio" in checkpoint.keys():
                    self.input_down_ratio = checkpoint['input_down_ratio']

                net_name = detection_type_dict[detection_type]

                threshold = self.cfg.FunctionThreshold["default"]
                if self.function_name in self.cfg.FunctionThreshold.keys():
                    threshold = self.cfg.FunctionThreshold[self.function_name]


                system_log.WriteLine(f"[{self.function_name}]    load deep learning model from {model_path} with device: {self.device}, version: {version}, epoch: {epoch}, threshold: {threshold}, detection_type: {detection_type}, net_name: {net_name}, input_down_ratio: {self.input_down_ratio}")

                self.model = Detector(net_name, num_class, self.golden_sample_path, threshold, self.device, self.cfg)
                self.model.load_model(state_dict)
                return 0, version


            elif os.path.exists(m_path):
                model_path = m_path
                self.type = "SVM"
                self.model = SVMModel(self.function_name)
                self.model.load_model(model_path)
                return 0, version
            else:
                system_log.WriteLine(f"ERROR: load model error. no model in {model_root}")
                return -1, version

        except:
            exstr = traceback.format_exc()
            system_log.WriteLine(f"{exstr}")
            return -1 
        
    def predict(self, cv_img):
        try:
            if self.type == "DL":
                if not self.input_down_ratio == 1:
                    cv_img = cv.resize(cv_img, None, fx=1/self.input_down_ratio, fy=1/self.input_down_ratio, interpolation=cv.INTER_AREA)

                result, cost_time_or_error_info = self.model.predict(cv_img) 
                system_log.WriteLine(f"[{self.function_name}]    predict image done. result: {result}, {cost_time_or_error_info}")

                if (self.cfg.System["debug"] == 1 or self.cfg.System["debug"] == True) and self.log_root is not None:
                    temp_img = cv_img.copy()
                    debug_img_path = os.path.join(self.log_root, f"{self.function_name}.jpg")
                    for ret in result:
                        bbox = ret["bbox"]
                        score = ret["score"]
                        cv.rectangle(temp_img, (int(bbox[0]), int(bbox[1])), (int(bbox[0]+bbox[3]), int(bbox[1]+bbox[2])), (0,0,255), thickness=2)
                        cv.putText(temp_img, f"score={score:.2f}", (int(bbox[0]+5), int(bbox[1]+12)), cv.FONT_HERSHEY_SIMPLEX, 0.4, (0,0,255), thickness=1)
                    cv.imwrite(debug_img_path, temp_img)

                if len(result) > 0:
                    return 1
                else: 
                    return 0

            elif self.type == "SVM":
                result = self.model.predict(cv_img)
                return int(result)
        
        except:
            exstr = traceback.format_exc()
            system_log.WriteLine(f"{exstr}")
            return 0

