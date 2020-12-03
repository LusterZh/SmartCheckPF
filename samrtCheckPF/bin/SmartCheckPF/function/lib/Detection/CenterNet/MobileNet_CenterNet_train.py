import torch 
import os 

from ...Logger import system_log

from .dataset.dataLoader import dataset_loader
from torchvision import transforms

class MobileNet_CenterNet_Train(object):
    def __init__(self):
        pass 
    def train(self):
        pass 
    def val(self):
        pass 
    def save(self):
        pass 

def save_model(detection_type, model, input_down_ratio, model_path, version, epoch):
    if isinstance(model, torch.nn.DataParallel):
        state_dict = model.module.state_dict()
    else:
        state_dict = model.state_dict()

    data = {
        "detection_type": detection_type,
        "state_dict": state_dict,
        "version": version,
        "epoch": epoch,
        "input_down_ratio": input_down_ratio
    } 

    torch.save(data, model_path)

    system_log.WriteLine(f"save model to {model_path}")

def MobileNet_CenterNet_Train(detection_type, train_folder, val_folder, label, function_crop_size, trainsform=None, input_down_ratio=1):
    
    pass 