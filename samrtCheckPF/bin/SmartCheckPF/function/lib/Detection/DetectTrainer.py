import torch 

from ..config import system_config


class DetectTrainer(object):
    def __init__(self, net_name, num_class, detect_type, train_folder, val_folder, model_root, version=None):
        self.num_class = num_class
        self.detect_type = detect_type
        self.train_folder = train_folder
        self.val_folder = val_folder
        self.model_root = model_root
        self.version = version

    def train(self, max_epoch, base_lr, pretrained_model=None):
        pass 

