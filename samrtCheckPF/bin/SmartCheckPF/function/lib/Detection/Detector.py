from .CenterNet.MobileNet_CenterNet import MobileNet_CenterNet
from .YOLO.YOLOv3 import yolov3

class Detector(object):
    def __init__(self, net_name, num_class, golden_sample_path, threshold=None, device='cpu', config=None):
        self.model = None 
        self.threshold = threshold
        self.golden_sample_path = golden_sample_path

        if net_name == "MobileNet_CenterNet":
            self.model = MobileNet_CenterNet(num_class, threshold, device)
        elif net_name == "YOLOv3":
            self.model = yolov3(num_class, threshold, device, config)
            

    def load_model(self, state_dict):
        self.model.load_model(state_dict)

    def predict(self, cv_img):
        result, cost_time_or_error_info = self.model.predict(cv_img)

        return result, cost_time_or_error_info