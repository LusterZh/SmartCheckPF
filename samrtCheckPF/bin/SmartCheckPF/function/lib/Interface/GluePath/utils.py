import numpy as np
import cv2 as cv
import time
import os

from ...config import client_config

def ExtractROI(image, image_roi):
    x,y,h,w = image_roi
    img_shape = image.shape
    if len(img_shape) == 3:
        roi = image[y:y+h, x:x+w, :]
    else:
        roi = image[y:y+h, x:x+w]

    return roi


def Split(img):   
    roi_list = client_config.GluePathInspection["roi_list"] 
    img_list = []

    for roi in roi_list:
        patch = ExtractROI(img, roi)
        img_list.append(patch)

    return img_list

def ReplaceROI(src_image, patch_image, image_roi):
    x,y,h,w = image_roi
    img_shape = src_image.shape
    if len(img_shape) == 3:
        src_image[y:y+h, x:x+w, :] = patch_image
    else:
        src_image[y:y+h, x:x+w] += patch_image

    src_image[y:y+h, x:x+w] = np.where(src_image[y:y+h, x:x+w] > 0, 255, 0)

def Merge(img_list):
    roi_list = client_config.GluePathInspection["roi_list"] 
  
    if len(img_list[0].shape) == 3:
        black_img = np.zeros([3648,5472,3])
    else:
        black_img = np.zeros([3648,5472])

    for idx in range(len(img_list)):
        ReplaceROI(black_img, img_list[idx], roi_list[idx])
        
    return black_img

def MergeImg(image, mask):
    R = np.zeros_like(mask)
    B = np.zeros_like(mask)
    merged_mask = cv.merge([B, mask, R])
    final_img = image*0.9 + merged_mask*0.1
    return final_img

def ModifyShowBox(box, length):
    new_box = []

    for b in box:
        ct_x = (b['left']+b['right'])/2
        ct_y = (b['top']+b['bottom'])/2

        new_b = {}
        new_b['left'] = int(ct_x - (length//2))
        new_b['top']  = int(ct_y - (length//2))
        new_b['right'] = int(ct_x + (length//2))
        new_b['bottom'] = int(ct_y + (length//2))

        new_box.append(new_b)

    return new_box

def Box2Dict(box):
    x,y,h,w = box
    output = {}
    output['left'] = x
    output['top'] = y 
    output['right'] = x + w 
    output['bottom'] = y + h 

    return output 