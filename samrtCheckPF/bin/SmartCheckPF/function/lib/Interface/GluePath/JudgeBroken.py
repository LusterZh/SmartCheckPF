# -*- coding:utf-8 -*-
# v 2.6
# supplement region judgements.
import numpy as np
import cv2
from skimage import morphology
import math

# interface class
class JudgeBroken:
    
    def __init__(self, split, **params):
        self.judge_broken = globals()[f'{split}_broken'](**params).judge_broken

# father class
class _JudgeBroken(object):

    def __init__(self, **params):
        super(_JudgeBroken, self).__init__()
        for k, v in params.items():
            self.__setattr__(k, v)
        
    def binarize(self):
        self.mask[self.mask < self.th] = 0
        self.mask[self.mask >= self.th] = 1

    # downsample and tackle the edge
    def down_sample_and_fine_edge(self, mask):
        w, h = mask.shape[::-1]
        self.mask = np.pad(np.uint8(cv2.resize(mask, (w // self.scale, h // self.scale), interpolation=cv2.INTER_AREA)), (1, 1), 'constant', constant_values=0)

    # remove the small regions by connectedComponents    
    def remove_small_regions(self):
        mask = np.zeros_like(self.mask)
        _, connectedMask, M, _ = cv2.connectedComponentsWithStats(self.mask)
        for i in range(1, len(M)):
            if M[i, 4] > self.minSize:
                mask[connectedMask == i] = 1
        self.mask = mask

    # def is_legal(self, pos):

    #     (x, y), (b_x, b_y) = pos, self.mask.shape
    #     return True if x >= 0 and x < b_x and y >= 0 and y < b_y else False
    
    def extract_bones(self):
        self.bones = np.uint8(morphology.skeletonize(self.mask))

    # implement connected detection on bones
    def connected_bones_detection_by_sort(self):
        _, self.connectedBones, M, _ = cv2.connectedComponentsWithStats(self.bones)
        # sort the bone regions by its size (if the num of region is too large, it could make a huge cost)
        sort_region = {k : v for k, v in sorted({i : M[i][4] for i in range(1, len(M))}.items(), key=lambda x : x[1], reverse=True)}
        self.vals = list(sort_region.keys())[:self.maxRegions]

    def eight_neighbors(self, x, y, pre):
        eight = [
            ((x + 1, y + 1), ((x, y + 1), (x + 1, y))), 
            ((x - 1, y + 1), ((x, y + 1), (x - 1, y))), 
            ((x - 1, y - 1), ((x, y - 1), (x - 1, y))), 
            ((x + 1, y - 1), ((x, y - 1), (x + 1, y))),
            ((x, y - 1), None), ((x, y + 1), None), 
            ((x + 1, y), None), ((x - 1, y), None)
        ]
        self.eight = list(filter(lambda p : p[0] != pre, eight))

    def make_box(self, x, y):
        x, y, half = int(y - 1), int(x - 1), self.edge // 2 // self.scale
        return {
            'left' : (x - half) * self.scale, 'top' : (y - half) * self.scale, 
            'right' : (x + half) * self.scale, 'bottom' : (y + half) * self.scale
        }

    # To match the filter
    def inRegions(self, broken):

        b1, b2, _ = broken
        bx1, by1 = (b1['left'] + b1['right']) / 2, (b1['top'] + b1['bottom']) / 2
        bx2, by2 = (b2['left'] + b2['right']) / 2, (b2['top'] + b2['bottom']) / 2
        inReigon = lambda bx, by, r : bx >= r[0] and bx <= r[0] + r[2] and by >= r[1] and by <= r[1] + r[3]
        for region in self.regions:
            if inReigon(bx1, by1, region) or inReigon(bx2, by2, region):
                return False
        return True

    def analyze_brokens(self):

        nbs, lines, points = [], [], []
        for idx1 in range(len(self.brokens)):
            for idx2 in range(idx1 + 1, len(self.brokens)):
                b1, no1 = self.brokens[idx1]
                b2, no2 = self.brokens[idx2]
                bx1, by1 = (b1['left'] + b1['right']) / 2, (b1['top'] + b1['bottom']) / 2
                bx2, by2 = (b2['left'] + b2['right']) / 2, (b2['top'] + b2['bottom']) / 2
                if no1 != no2:
                    dist = math.sqrt((bx1 - bx2) ** 2 + (by1 - by2) ** 2)
                    lines.append((idx1, idx2, dist))
        if len(lines) > 0:
            lines = sorted(lines, key=lambda x : x[2])
            for line in lines:
                p1, p2, dist = line
                if p1 not in points and p2 not in points:
                    points.extend([p1, p2])
                    nbs.append([self.brokens[p1][0], self.brokens[p2][0], dist])
        else:
            dist = math.sqrt((bx1 - bx2) ** 2 + (by1 - by2) ** 2)
            nbs.append([self.brokens[0][0], self.brokens[1][0], dist])

        return nbs

# children class
class sofia_broken(_JudgeBroken):

    def __init__(self, **params):
        super(sofia_broken, self).__init__(**params)
        # supplement regions
        self.regions = [[4911, 1535, 170, 170], [4919, 2145, 170, 170]]

    def judge_broken(self, mask):
        self.down_sample_and_fine_edge(mask)
        self.brokens = []
        self.binarize()
        self.remove_small_regions()
        self.extract_bones()
        self.connected_bones_detection_by_sort()
        for val in self.vals:
            self.backup = self.connectedBones == val
            # first process finds a broken point for a single bone
            ret = self.process(self.getInitialPoint(val), val)
            if ret is None: return 0, None
            elif ret[1] is None: return ret
            self.connectedBones[self.backup] = val
            # second process finds another broken point for that bone
            self.process(ret, val)
        
        # for empty image (eg. id card)
        if len(self.brokens) == 0:
            return (0, None)

        else:
            self.brokens = self.analyze_brokens() if len(self.brokens) > 1 else self.brokens
            self.brokens = list(filter(self.inRegions, self.brokens))
            return (0, self.brokens) if len(self.brokens) > 0 else (1, None)
        
    def getInitialPoint(self, val):
        pointset = np.argwhere(self.connectedBones == val)
        return tuple(pointset[len(pointset) // 2])
    
    # enhanced DFS
    # judges the seg path is a closed cycle or not and finds the broken point
    # return None                 if unexpected situation occurs that it can't find the broken point     
    # return 1, None              if the glue path is a closed loop and its length over minLength
    # return (x, y)               if it finds the broken point (x, y)                                       
    def process(self, initialpoint, val):
        endpoint, paths, max_step = None, [(initialpoint, initialpoint, 0)], 0
        self.connectedBones[initialpoint], self.step_dict = -1, {initialpoint : 0}
        while len(paths):
            ans, (pre, (x, y), step) = 0, paths.pop()
            self.eight_neighbors(x, y, pre)
            for (next, directs) in self.eight:
            # if self.is_legal(next):
                ret = self.process_step(val, next, (x, y), paths, step, directs)
                if ret == -1 : return 1, None
                elif ret != -1: ans += ret
            if ans == 0 and step > max_step:
                endpoint = (x, y)
                max_step = step
        if endpoint is not None: self.brokens.append([self.make_box(*endpoint), val])
        return endpoint

    # the step of the DFS
    def process_step(self, val, next, cur, paths, step, directs=None):
        ret = None
        if directs is None: ret = self.process_unit(val, next, cur, paths, step)
        elif directs is not None and self.connectedBones[directs[0]] != 1 and self.connectedBones[directs[1]] != 1: ret = self.process_unit(val, next, cur, paths, step)
        return 1 if ret is None else ret

     # the basic unit of the DFS step
    def process_unit(self, val, next, cur, paths, step):
        if next in self.step_dict and self.connectedBones[next] == -1 and step - self.step_dict[next] > self.minLength: return -1
        elif self.connectedBones[next] == val:
            self.connectedBones[next] = -1
            self.step_dict[next] = step + 1
            paths.append((cur, next, step + 1))
            return 1
        elif self.connectedBones[next] == 0: return 0
        if endpoint is not None: self.brokens.append([self.make_box(*endpoint), val])
        return endpoint

    # the step of the DFS
    def process_step(self, val, next, cur, paths, step, directs=None):
        ret = None
        if directs is None: ret = self.process_unit(val, next, cur, paths, step)
        elif directs is not None and self.connectedBones[directs[0]] != 1 and self.connectedBones[directs[1]] != 1: ret = self.process_unit(val, next, cur, paths, step)
        return 1 if ret is None else ret

     # the basic unit of the DFS step
    def process_unit(self, val, next, cur, paths, step):
        if next in self.step_dict and self.connectedBones[next] == -1 and step - self.step_dict[next] > self.minLength: return -1
        elif self.connectedBones[next] == val:
            self.connectedBones[next] = -1
            self.step_dict[next] = step + 1
            paths.append((cur, next, step + 1))
            return 1
        elif self.connectedBones[next] == 0: return 0
        
if __name__ == '__main__':
    
    jb = JudgeBroken('sofia', th=127, edge=100, minLength=5000, scale=2, maxRegions=5, minSize=1500)
    img = cv2.cvtColor(cv2.imread('C:/luster/StandardSmartCheckPF/samrtCheckPF/bin/SmartCheckPF/function/mask.png'), cv2.COLOR_BGR2GRAY)
    ans, boxes = jb.judge_broken(img)
    print (f'ans : {ans} | boxes : {boxes}')
    # ans, boxes = jb.judge_broken(mask)