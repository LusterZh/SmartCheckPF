from __future__ import absolute_import
from __future__ import division
from __future__ import print_function

import cv2 as cv 
from skimage import feature as skft
import numpy as np 

def equalizeHistImg(img):
    (b, g, r) = cv.split(img)
    bH = cv.equalizeHist(b)
    gH = cv.equalizeHist(g)
    rH = cv.equalizeHist(r)
    result = cv.merge((bH, gH, rH))
    return result

def create_lbp_hist(image):
    if len(image.shape) == 3:
        gray_img = cv.cvtColor(image, cv.COLOR_RGB2GRAY)
    else:
        gray_img = image

    lbp_image = skft.local_binary_pattern(gray_img, 8, 1, 'default')
    hist = np.bincount(lbp_image.astype(np.int64).flatten(), minlength=256)

    return hist.astype(np.float64)

def create_rgb_hist_(image):
    b = image[:,:,0]
    g = image[:,:,1]
    r = image[:,:,2]

    b = (b / 16).astype(np.int64)
    g = (g / 16).astype(np.int64)
    r = (r / 16).astype(np.int64)

    image_bgr = b*16*16 + g*16 + r
    hist = np.bincount(image_bgr.flatten(), minlength=4096)

    return hist.astype(np.float64)

def create_rgb_hist(image):
    hist = cv.calcHist(image, [0], None, [256], [0, 255])
    hist = hist.flatten()
    return hist