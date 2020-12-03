import cv2
import os
import numpy as np
import threading
import queue

# v 2.0
# modularization

class Registration:
    
    def __init__(self, params=None, golden=None):
        if params is not None:
            for key, value in params.items():
                self.__setattr__(key, value)

            if golden is not None:
                getattr(self, f'{self.method}_init')(golden)
                golden = None

    def SURF_init(self, golden):
        self.detector = cv2.xfeatures2d_SURF.create()
        self.matcher = cv2.BFMatcher()
        x, y, w, h = self.roiConvert(self.region)
        self.offset = np.float32([y, x])
        region = golden[x : x + w, y : y + h]
        region = cv2.resize(region, None, None, 1./self.scale, 1./self.scale)
        self.kp, self.des = self.detector.detectAndCompute(region, None)
        region = None

    def HOUGH_init(self, golden):
        self.gcnts, _ = self.getCnts(golden, self.boxes)
        self.gcnts = np.array(self.gcnts).reshape(-1, 1, 2)

    def get_GMatch(self, des):
        matches = self.matcher.knnMatch(self.des, des, k=2)
        good = sorted([m for m, n in matches if m.distance < self.delta * n.distance], key=lambda k : k.distance)
        return good

    def getCnts(self, img, boxes):
        cnts, stats = [], np.array([False] * len(boxes))
        for idx, box in enumerate(boxes):
            x, y, w, h = box
            chunk = cv2.cvtColor(img[x : x + w, y : y + h], cv2.COLOR_BGR2GRAY)
            cnt = cv2.HoughCircles(chunk, cv2.HOUGH_GRADIENT, 1, max(w, h), minRadius=self.minRadius[idx], maxRadius=self.maxRadius[idx], param1=self.param1, param2=self.param2)
            if cnt is not None:
                cnt, radius = np.float32(cnt.reshape(-1)[:2]), np.float(cnt.reshape(-1)[-1])
                if not (cnt == np.array([0, 0], dtype=np.float32)).all():
                    cnts.append(cnt + np.array([y, x], dtype=np.float32))
                    stats[idx] = True

        return cnts, stats

    def getH(self, image):
        try:
            if self.method == 'SURF':
                kp, des = self.detector.detectAndCompute(cv2.resize(image, None, None, 1./self.scale, 1./self.scale), None)
                GMatch = self.get_GMatch(des)
                ptsA = np.float32([self.kp[m.queryIdx].pt for m in GMatch]).reshape(-1, 1, 2) * self.scale + self.offset
                ptsB = np.float32([kp[m.trainIdx].pt for m in GMatch]).reshape(-1, 1, 2) * self.scale
                H = cv2.estimateRigidTransform(ptsB, ptsA, fullAffine=True)
                return H

            elif self.method == 'HOUGH':
                cnts, stats = self.getCnts(image, self.boxes)
                if len(cnts) < 3:
                    return None
                cnts = np.array(cnts).reshape(-1, 1, 2)
                gcnts = self.gcnts[stats]
                H = cv2.estimateRigidTransform(cnts, gcnts, fullAffine=True)
                return H

        except:
            return None
        
    @staticmethod
    def roiConvert(roi):
        return (*roi[:2][::-1], *roi[2:])

    @staticmethod
    def affineTransform(point, H):
        p = np.array([*point[::-1], 1])
        coords = np.dot(H, p)
        if len(coords) == 2:
            ax, ay = coords

        else:
            ax, ay, aw = coords
            ax /= aw
            ay /= aw
        return (int(max(ay, 0)), int(max(ax, 0)))

    def getRoi(self, roi, H=None):
        try:
            x, y, w, h = self.roiConvert(roi)
            H = np.array(H, dtype=np.float32)
            H = self.invert(H)
            points = np.array([self.affineTransform(p, H) for p in [[x, y], [x, y + h], [x + w, y], [x + w, y + h]]])
            minx, miny, maxx, maxy = np.min(points[:, 0]), np.min(points[:, 1]), np.max(points[:, 0]), np.max(points[:, 1])
            cx, cy = (minx + maxx) / 2, (miny + maxy) / 2
            x, y, w, h = int(max(cx - w / 2, 0)), int(max(cy - h / 2, 0)), w, h
            return self.roiConvert((x, y, w, h))

        except: 
            return roi
    
    @staticmethod
    def invert(H):
        if H.shape == (2, 3):
            return cv2.invertAffineTransform(H)

        elif H.shape == (3, 3):
            return np.linalg.pinv(H)

    def getPatchAndRoi(self, image, roi, H=None):
        try:
            H = np.array(H)
            nroi = self.getRoi(roi, H.copy())
            x, y, w, h = self.roiConvert(nroi)
            shape = image.shape[:2]
            assert x > 0 and y > 0 and x + w < shape[0] and y + h < shape[1]

        except:
            nroi = roi

        x, y, w, h = self.roiConvert(nroi)
        patch = image[x : x + w, y : y + h].copy()
        image = None

        return patch, nroi

    def getGROI(self, H, roi):
        try:
            H = np.array(H)
            return self.getRoi(roi, cv2.invertAffineTransform(H))

        except:
            return roi

#############################################################################################
# interfaces

#############################################################################################
# multi-threading processing functions
def multiThreadingProcess(ImageIds, rets, params, function, workers=50):
    q = queue.Queue(workers)
    for tid, param in enumerate(params):
        t = threading.Thread(target=function, args=(rets, tid, *param))
        t.start()
        q.put(t)
        if q.full():
            while not q.empty():
                t = q.get()
                t.join()
    while not q.empty():
        t = q.get()
        t.join()

    return ImageIds, rets

def registration_extract_patchs_and_rois(imageIds, Hs, imagePaths, dstPaths, roi_rect, workers=50):
    rets = [None] * len(imagePaths)
    params = ([imagePath, dstPath, H, roi_rect] for imagePath, dstPath, H in zip(imagePaths, dstPaths, Hs))
    return multiThreadingProcess(imageIds, rets, params, registration_extract_patchs_and_rois_unit, workers=workers)

def registration_extract_patchs_and_rois_unit(rets, tid, imagePath, dstPath, H, roi_rect):
    reg = Registration()
    image = cv2.imread(imagePath)
    patch, roi = reg.getPatchAndRoi(image, roi_rect, H)
    rets[tid] = roi
    cv2.imwrite(dstPath, patch)

def registration_extract_rois(imageIds, Hs, imagePaths, roi_rect, workers=50):
    rets = [None] * len(imagePaths)
    params = ([imagePath, H, roi_rect] for imagePath, H in zip(imagePaths, Hs))
    return multiThreadingProcess(imageIds, rets, params, registration_extract_rois_unit, workers=workers)

def registration_extract_rois_unit(rets, tid, imagePath, H, roi_rect):
    reg = Registration()
    roi = reg.getRoi(roi_rect, H)
    rets[tid] = roi

def calculate_Hs(reg_params, goldensample, imageIds, imagePaths, workers=50):
    goldensample = cv2.imread(goldensample)
    rets = [None] * len(imagePaths)
    params = ([reg_params, goldensample, imagePath] for imagePath in imagePaths)
    return multiThreadingProcess(imageIds, rets, params, calculate_Hs_unit, workers=workers)

def calculate_Hs_unit(rets, tid, reg_params, goldensample, imagePath):
    reg = Registration(reg_params, goldensample)
    image = cv2.imread(imagePath)
    H = reg.getH(image)
    try:
        rets[tid] = H
    except:
        rets[tid] = None

#############################################################################################
# single operations

def registration_extract_single_patch_and_roi(imageId, imagePath, dstPath, H, roi_rect):
    reg = Registration()
    thd = threading.Thread(target=registration_extract_single_patch, args=(imagePath, dstPath, H, roi_rect, reg))
    thd.start()
    roi = reg.getRoi(roi_rect, H)
    return imageId, roi

def registration_extract_single_patch(imagePath, dstPath, H, roi_rect, reg=None):
    reg = Registration() if reg is None else reg
    image = cv2.imread(imagePath)
    patch, _ = reg.getPatchAndRoi(image, roi_rect, H)
    cv2.imwrite(dstPath, patch)

def registration_extract_single_roi(imageId, imagePath, H, roi_rect):
    reg = Registration()
    roi = reg.getRoi(roi_rect, H)
    return imageId, roi

def calculate_H(reg_params, goldensample, imageId, imagePath):
    goldensample = cv2.imread(goldensample)
    image = cv2.imread(imagePath)
    reg = Registration(reg_params, goldensample)
    H = reg.getH(image)
    try:
        H = H.tolist()
        return imageId, H
    except:
        return imageId, None