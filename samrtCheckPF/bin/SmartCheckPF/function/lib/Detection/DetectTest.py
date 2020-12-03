from .Detector import Detector
import os 
import torch
import cv2 as cv

class DetectTest(object):
    def __init__(self, net_name, num_class, golden_sample_path, model_path, threshold=None, device='cpu', cfg=None):
        self.model = Detector(net_name, num_class, golden_sample_path, threshold, device, cfg)

        if device == 'cpu':
            checkpoint = torch.load(model_path, map_location='cpu')
        else:
            checkpoint = torch.load(model_path)

        state_dict = checkpoint['state_dict']
        self.model.load_model(state_dict)

    def run(self, img_folder):
        for file_name in filter(lambda x: ".jpg" in x, os.listdir(img_folder)):
            img_path = os.path.join(img_folder, file_name)
            cv_img = cv.imread(img_path)
            result, cost_time_info = self.model.predict(cv_img)

            print(f"predict image {file_name}, result: {result}, cost_time: {cost_time_info}")

