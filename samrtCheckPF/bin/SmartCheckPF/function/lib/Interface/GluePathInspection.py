import os 
import torch 
import time
import cv2 as cv 
import numpy as np

from ..Segmenteation.Segment import Segment
from .GluePath.utils import Split, Merge, MergeImg, ModifyShowBox, Box2Dict
from .GluePath.JudgeBroken import JudgeBroken

from ..Logger import system_log

class GluePathInspection(object):
    def __init__(self, net_name, device):
        self.judge = JudgeBroken("sofia", th=127, edge=100, minLength=5000, scale=2, maxRegions=5, minSize=1500)
        self.net_name = net_name
        self.model = Segment(net_name, device)

    def load_model(self, model_path):
        self.model.load_model(model_path) 

    def predict(self, cv_img, MaskPath, MergeImgPath):
        start_time = time.time()

        if self.net_name == "STM":
            seg_img = cv_img[...,::-1]          # only STM
        else:
            seg_img = cv_img

        img_list = Split(seg_img)
        
        img_list[-2] = np.rot90(img_list[-2], -1)       # img_l
        img_list[-1] = np.rot90(img_list[-1])           # img_r
        split_time = time.time()

        mask_list = []

        for img in img_list:
            mask_ = self.model.predict(img)
            mask_list.append(mask_)

        forward_time = time.time()

        mask_list[-2] = np.rot90(mask_list[-2])              # mask_l
        mask_list[-1] = np.rot90(mask_list[-1], -1)         # mask_r

        mask = Merge(mask_list)
        merge_img = MergeImg(cv_img, mask)
        cv.imwrite(MaskPath, mask)
        cv.imwrite(MergeImgPath, merge_img)
        system_log.WriteLine(f"write image to {MaskPath}")
        merge_time = time.time()

        system_log.WriteLine(f"seg predict done. cost time: split_time: {(split_time-start_time):.8f}sec, forward_time: {(forward_time-split_time):.8f}sec, merge_time: {(merge_time-forward_time):.8f}sec")

        ans, boxes = self.judge.judge_broken(mask)
        system_log.WriteLine(f"judge broken result: ans: {ans}, boxes: {boxes}")

        # filter the boxes
        new_boxes = []
        if ans == 0 and boxes is not None:
            for box in boxes:
                box1, box2, gap = box 
                if gap >= 100:
                    new_boxes.append(box1)
                    new_boxes.append(box2)
                elif gap < 100 and gap > 10:
                    ct1_x = (box1['left']+box1['right'])/2
                    ct1_y = (box1['top']+box1['bottom'])/2
                    ct2_x = (box2['left']+box2['right'])/2
                    ct2_y = (box2['top']+box2['bottom'])/2
                    ct_x = (ct1_x+ct2_x)/2
                    ct_y = (ct1_y+ct2_y)/2
                    roi_box = [int(ct_x-50), int(ct_y-50), 100, 100]
                    new_boxes.append(Box2Dict(roi_box))
                else:
                    continue

            if len(new_boxes) == 0:
                ans = 1
            else:
                ans = 0
        
        new_boxes = ModifyShowBox(new_boxes, 200)

        # ans=1: pass    ans=0: fail
        return ans, new_boxes




        