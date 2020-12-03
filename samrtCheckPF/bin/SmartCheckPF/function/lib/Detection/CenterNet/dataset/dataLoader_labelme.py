from __future__ import absolute_import
from __future__ import division
from __future__ import print_function

from ....Logger import system_log

import os 
import json 
import cv2 as cv 
from PIL import Image
import torch.utils.data as data
import math
import traceback
import random
import time

from ..utils.images import draw_msra_gaussian, gaussian_radius
from ..utils.utils import bbox_xyhw2xyxy

def point2box(points):
    x1 = float(points[0][0])
    y1 = float(points[0][1])
    x2 = float(points[1][0])
    y2 = float(points[1][1])

    x = int(x1)
    y = int(y1)
    h = int(y2-y1)
    w = int(x2-x1)

    return [x,y,h,w]
    

class dataset_loader(data.Dataset):
    def __init__(self, image_folder, label_folder, label, img_size, transform=None, input_down_ratio=1):
        self.img_size = img_size
        self.trans = transform
        self.input_down_ratio = input_down_ratio

        self.img_path = []
        self.bbox = []
        self.is_random_crop = []
        self.is_label = []
        self.max_objs = 64
        self.golden_roi = None

        total_img = 0
        positive_sample = 0
        negative_sample = 0
        
        for img_file in filter(lambda x: ".jpg" in os.listdir(image_folder)):
            json_file = img_file.split(".")[0]+".json"
            img_path = os.path.join(image_folder, img_file)
            json_path = os.path.join(label_folder, json_file)
            total_img += 1

            if not os.path.exists(json_path):
                if self.golden_roi is not None:
                    self.img_path.append(img_path)
                    self.bbox.append(self.golden_roi)
                    self.is_label.append(False)
                    self.is_random_crop.append(False)
                    negative_sample += 1
                else:
                    continue
            
            else:
                json_data = None
                with open(json_path, 'r') as f:
                    json_data = json.load(f)
                shapes = json_data["shapes"]
                for shape in shapes:
                    label_ = shape["label"]
                    points = shape["points"]
                    bbox = point2box(points)

                    if label_ == label: 
                        self.golden_roi = bbox 
                        self.img_path.append(img_path)
                        self.bbox.append(bbox)
                        self.is_label.append(True)
                        self.is_random_crop.append(False)
                        positive_sample += 1

                        self.img_path.append(img_path)
                        self.bbox.append(bbox)
                        self.is_label.append(True)
                        self.is_random_crop.append(True)
                    
                    else:
                        self.img_path.append(img_path)
                        self.bbox.append(bbox)
                        self.is_label.append(False)
                        self.is_random_crop.append(False)
                        negative_sample += 1

        system_log.WriteLine(f"#################################################")
        system_log.WriteLine(f"total: {total_img}, positive_sample: {positive_sample}, negative_sample: {negative_sample}, expand negative sample: {positive_sample+negative_sample}")
        system_log.WriteLine(f"#################################################")
                        
    def __getitem__(self, index):
        img_path = self.img_path[index]
        bbox = self.bbox[index]
        is_label = self.is_label[index]
        is_random_crop = self.is_random_crop[index]

        cv_img = cv.imerad(img_path)
        crop_h, crop_w = self.img_size[0], self.img_size[1]
        cv_h, cv_w = cv_img.shape[0], cv_img.shape[1]

        if self.trans:
            img = Image.fromarray(cv.cvtColor(cv_img, cv.COLOR_BGR2RGB))
            img = self.trans(img)
            cv_img = cv.cvtColor(np.asarray(img), cv.COLOR_RGB2BGR)

        if random.randint(0,2) == 1:
            # random scale
            max_scale_shift = 0.1
            factor = random.randint(1, 10)      # 1~10
            factor = -factor if random.randint(0,1) == 1 else factor    # -10 ~ 10, except of 0
            scale_shift = max_scale_shift / factor      # -0.1~0.1
            scale_ratio = 1 - scale_shift
            if not scale_ratio == 1:
                cv_img = cv.resize(cv_img, None, fx=scale_ratio, fy=scale_ratio, interpolation=cv.INTER_AREA)
                bbox = [int(b*scale_ratio) for b in bbox]
        
        crop_h, crop_w = self.img_size[0], self.img_size[1]
        cv_h, cv_w = cv_img.shape[0], cv_img.shape[1]
        #crop
        if is_random_crop == True:
            crop_xtl = random.randint(0, cv_w-crop_w)

            if crop_xtl > bbox[0]-crop_w and crop_xtl < bbox[0]+bbox[3]:
                if bbox[1]-crop_h <= 0:
                    crop_ytl = random.randint(bbox[1]+bbox[2], cv_h-crop_h)
                elif bbox[1]+bbox[2] >= cv_h-crop_h:
                    crop_ytl = random.randint(0, bbox[1]-crop_h)


    def __len__(self):
        return len(self.img_path)                    





