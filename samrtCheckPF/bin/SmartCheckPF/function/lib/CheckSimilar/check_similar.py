from __future__ import absolute_import
from __future__ import division
from __future__ import print_function

import cv2 as cv
import numpy as np
from skimage import feature as skft

class CheckSimilar(object):
    def __init__(self, is_hog=True, is_rgb_hist=True, is_sift=True, is_lbp=True):
        self.is_hog = is_hog
        self.is_rgb_hist = is_rgb_hist
        self.is_sift = is_sift
        self.is_lbp = is_lbp

        self.sift = cv.xfeatures2d.SIFT_create()
        self.hog = cv.HOGDescriptor()

    def Initial_img(self, cv_golden_sample):
        self.golden_sample = cv.resize(cv_golden_sample, (128,128))
        # self.golden_sample = self.pre_resize(self.golden_sample)
        self.golden_sample_gray = cv.cvtColor(self.golden_sample, cv.COLOR_BGR2GRAY)
        self.golden_sobel = cv.Sobel(self.golden_sample_gray, cv.CV_16S, 1, 0)
        self.golden_sobel = cv.convertScaleAbs(self.golden_sobel)
        #self.golden_canny = cv.Canny(self.golden_sample_gray, 100, 200)

        if self.is_sift:
            try:
                self.golden_kps, self.golden_des = self.sift.detectAndCompute(self.golden_sobel, None)       # sift feature
                self.kps_num = self.golden_des.shape[0]
            except:
                self.is_sift = False
        if self.is_rgb_hist:
            self.golden_rgb_hist = self.create_rgb_hist(self.golden_sample)         # rgb hist
        if self.is_hog:
            try:
                if self.golden_sobel.shape[0]*self.golden_sobel.shape[1] > 1000*1000:
                    self.is_hog = False     # if golden_sample is too large, don't compute hog features.
                else:
                    self.golden_hog = self.hog.compute(self.golden_sobel)
                    if np.sum(self.golden_hog) == 0:
                        self.is_hog = False # if golden sample can't extract hog feature, don't calculate hog feature later.
            except:
                self.is_hog = False
        if self.is_lbp:
            self.golden_lbp_hist = self.create_lbp_hist(self.golden_sample)
    
    def create_rgb_hist(self, image):
        b = image[:,:,0]
        g = image[:,:,1]
        r = image[:,:,2]

        b = cv.equalizeHist(b)
        g = cv.equalizeHist(g)
        r = cv.equalizeHist(r)

        b = (b / 16).astype(np.int64)
        g = (g / 16).astype(np.int64)
        r = (r / 16).astype(np.int64)

        image_bgr = b*16*16 + g*16 + r
        hist = np.bincount(image_bgr.flatten(), minlength=4096)

        hist = hist.reshape(-1, 1)
        return hist.astype(np.float32)

    def create_lbp_hist(self, image):
        if len(image.shape) == 3:
            gray_img = cv.cvtColor(image, cv.COLOR_RGB2GRAY)
        else:
            gray_img = image

        lbp_image = skft.local_binary_pattern(gray_img, 8, 1, 'default')
        hist = np.bincount(lbp_image.astype(np.int64).flatten(), minlength=256)

        hist = hist.reshape(-1, 1)
        return hist.astype(np.float32)

    def compare_img(self, cv_target_sample):
        target_sample = cv.resize(cv_target_sample, (128,128))
        target_sample_gray = cv.cvtColor(target_sample, cv.COLOR_BGR2GRAY)
        target_sobel = cv.Sobel(target_sample_gray, cv.CV_16S, 1, 0)
        target_sobel = cv.convertScaleAbs(target_sobel)
        sift_similar = 0
        rgb_hist_similar = 0
        hog_similar = 0
        lbp_hist_similar = 0

        if self.is_sift:
            # sift  
            target_kps, target_des = self.sift.detectAndCompute(target_sobel, None)
            FLANN_INDEX_KDTREE = 1
            index_params = dict(algorithm=FLANN_INDEX_KDTREE, trees=5)
            search_params = dict(checks=50)
            flann = cv.FlannBasedMatcher(index_params, search_params)
            try:
                matches = flann.knnMatch(self.golden_des, target_des, k=2)          
                goodMatch = []
                for m, n in matches:
                    if m.distance < 0.90*n.distance:
                        goodMatch.append(m)
                sift_similar = len(goodMatch)/(self.kps_num+0.000001)
            except:
                sift_similar = 0

        if self.is_rgb_hist:
            # rgb hist
            target_rgb_hist = self.create_rgb_hist(target_sample)
            rgb_hist_similar = cv.compareHist(self.golden_rgb_hist, target_rgb_hist, cv.HISTCMP_CORREL)
            if rgb_hist_similar < 0:
                rgb_hist_similar = 0
    
        if self.is_hog:
            # hog features
            try:
                if target_sobel.shape[0]*target_sobel.shape[1] > 1000*1000:
                    hog_similar = 0     # if target sobel is too large, don't compute hog features
                else:
                    target_hog = self.hog.compute(target_sobel)
                    if np.sum(target_hog) == 0:
                        hog_similar = 0
                    else:
                        hog_similar = cv.compareHist(self.golden_hog, target_hog, cv.HISTCMP_CORREL)
                    if hog_similar < 0:
                        hog_similar = 0
            except:
                hog_similar = 0

        if self.is_lbp:
            # lbp features
            target_lbp = self.create_lbp_hist(target_sample)
            lbp_hist_similar = cv.compareHist(self.golden_lbp_hist, target_lbp, cv.HISTCMP_CORREL)
            if lbp_hist_similar < 0:
                lbp_hist_similar = 0

        similar = 0.31*sift_similar + 0.25*rgb_hist_similar + 0.31*hog_similar + 0.13*lbp_hist_similar
        return similar