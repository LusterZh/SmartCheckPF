from __future__ import absolute_import
from __future__ import division
from __future__ import print_function

from .features import *
from ..Logger import system_log

import cv2 as cv 
from sklearn import svm
from sklearn.decomposition import PCA
import pickle
import random 
import traceback
import time 


class SVMModel(object):
    def __init__(self, function_name, is_hog=True, is_rgb_hist=False, is_lbp=False, is_otsu=False):
        self.model = svm.SVC() 
        self.function_name = function_name
        self.hog = cv.HOGDescriptor()
        self.is_hog = is_hog
        self.is_rgb_hist = is_rgb_hist
        self.is_lbp = is_lbp
        self.is_otsu = is_otsu
        self.pca = None 

    def load_model(self, model_path):
        try:
            with open(model_path,'rb') as infile:
                loaded = pickle.load(infile)
                self.model = loaded['model']
                self.pca = loaded['pca']
                self.is_hog = loaded['is_hog']
                self.is_rgb_hist = loaded['is_rgb_hist']
                self.is_lbp = loaded['is_lbp']
                if "is_otsu" in loaded.keys():
                    self.is_otsu = loaded['is_otsu'] 

                system_log.WriteLine(f"[{self.function_name}]  load SVM model done, is_hog: {self.is_hog}, is_rgb_hist: {self.is_rgb_hist}, is_lbp: {self.is_lbp}, is_otsu: {self.is_otsu}")

        except:
            exstr = traceback.format_exc()
            system_log.WriteLine(f"{exstr}")
            return None 

    def gasuss_noise(self, image, mean=0, var=0.001):
        image = np.array(image/255, dtype=float)
        noise = np.random.normal(mean, var ** 0.5, image.shape)
        out = image + noise
        if out.min() < 0:
            low_clip = -1.
        else:
            low_clip = 0.
        out = np.clip(out, low_clip, 1.0)
        out = np.uint8(out*255)
        return out
        

    def extract_features(self, cv_img):
        resize_img = cv.resize(cv_img, (128,128))
        equalizeImg = equalizeHistImg(resize_img)
        gray_img = cv.cvtColor(equalizeImg, cv.COLOR_BGR2GRAY)
        sobel_img = cv.Sobel(gray_img, cv.CV_16S, 1, 0)
        sobel_img = cv.convertScaleAbs(sobel_img)
        _, th_img = cv.threshold(gray_img, 0, 255, cv.THRESH_OTSU)

        if self.is_hog and not self.is_otsu:
            hog_fea = self.hog.compute(sobel_img).flatten()
        if self.is_hog and self.is_otsu:
            hog_fea = self.hog.compute(th_img).flatten()
        if self.is_lbp:
            lbp_fea = create_lbp_hist(gray_img)
        if self.is_rgb_hist and not self.is_otsu:
            rgb_fea = create_rgb_hist(resize_img)
        if self.is_rgb_hist and self.is_otsu:
            rgb_fea = create_rgb_hist(th_img)

        if self.is_hog and not self.is_rgb_hist and not self.is_lbp:
            features = hog_fea
        elif self.is_rgb_hist and not self.is_hog and not self.is_lbp:
            features = rgb_fea
        elif self.is_lbp and not self.is_hog and not self.is_rgb_hist:
            features = lbp_fea
        elif self.is_hog and self.is_rgb_hist and not self.is_lbp:
            features = np.concatenate((hog_fea, rgb_fea))
        elif self.is_hog and self.is_lbp and not self.is_rgb_hist:
            features = np.concatenate((hog_fea, lbp_fea))
        elif self.is_rgb_hist and self.is_lbp and not self.is_hog:
            features = np.concatenate((lbp_fea, rgb_fea))
        elif self.is_hog and self.is_rgb_hist and self.is_lbp:
            features = np.concatenate((hog_fea, lbp_fea, rgb_fea))
        else:
            features = hog_fea

        return features

    def predict(self, cv_img):
        try:
            start = time.time()
            features = self.extract_features(cv_img)
            if np.sum(features) == 0:
                return 0
            features = np.expand_dims(features, axis=0)
            features = self.pca.transform(features)
            
            result = self.model.predict(features)
            end = time.time()
            system_log.WriteLine(f"[{self.function_name}]   SVM predict done. result is {result[0]}, cost time: {(end-start):.8f}sec!")
            return result[0]
        except:
            exstr = traceback.format_exc()
            system_log.WriteLine(f"{exstr}")
            return 0 
        
    def train(self, img_list, select_list, model_path):
        system_log.WriteLine(f"[{self.function_name}]   svm training... img_list number is {len(img_list)}")
        X = []
        Y = [] 

        for idx in range(len(img_list)):
            img_path = img_list[idx]
            img = cv.imread(img_path)
            fea = self.extract_features(img)
            if np.sum(fea) == 0:
                continue
            X.append(fea)
            Y.append(select_list[idx])

            # expand samples
            # flip x
            img_flip_x = cv.flip(img, 1)
            fea = self.extract_features(img_flip_x)
            X.append(fea)
            Y.append(select_list[idx])

            # flip y
            img_flip_y = cv.flip(img, 0)
            fea = self.extract_features(img_flip_y)
            X.append(fea)
            Y.append(select_list[idx])

            # flip xy
            img_flip_xy = cv.flip(img, -1)
            fea = self.extract_features(img_flip_xy)
            X.append(fea)
            Y.append(select_list[idx])

            # gasussin
            img_gasuss = self.gasuss_noise(img)
            fea = self.extract_features(img_gasuss)
            X.append(fea)
            Y.append(select_list[idx])


        if len(X) == 0 or len(Y) == 0 or not len(X) == len(Y):
            system_log.WriteLine(f"ERROR: X and Y length must be equal.")
            return False

        try:
            system_log.WriteLine(f"[{self.function_name}]   expand samples and extract features done!")
            system_log.WriteLine(f"[{self.function_name}]   do PCA")
            X = np.array(X)
            n_components = 100 if 100 < X.shape[0] else X.shape[0]
            self.pca = PCA(n_components=n_components)
            # system_log.WriteLine(f"[{self.function_name}]   X shape is {X.shape}, Y shape is {np.array(Y).shape}")
            X = self.pca.fit_transform(X)
            system_log.WriteLine(f"[{self.function_name}]   X shape is {X.shape},   Y shape is {np.array(Y).shape}")
            system_log.WriteLine(f"[{self.function_name}]   hog: {self.is_hog}, rgb: {self.is_rgb_hist}, lbp: {self.is_lbp}, otsu: {self.is_otsu}")
            # print(f"X shape is {X.shape},   Y shape is {np.array(Y).shape}")

            self.model.fit(X, Y)
            
            with open(model_path,'wb') as outfile:
                pickle.dump({
                    'model': self.model,
                    'pca': self.pca,
                    'is_hog': self.is_hog,
                    "is_rgb_hist": self.is_rgb_hist,
                    "is_lbp": self.is_lbp,
                    "is_otsu": self.is_otsu
                },outfile)
            
            system_log.WriteLine(f"Train SVM done, save model to {model_path}")
                
        except:
            exstr = traceback.format_exc()
            system_log.WriteLine(f"{exstr}")
            return False

        return True