import numpy as np
import torch

class Compose(object):
    """
    Combine several transformation in a serial manner
    """

    def __init__(self, transform=[]):
        self.transforms = transform

    def __call__(self, imgs, annos):

        for m in self.transforms:
            imgs, annos = m(imgs, annos)

        return imgs, annos




class ToFloat(object):
    """
    convert value type to float
    """

    def __init__(self):
        pass

    def __call__(self, imgs, annos):
        for idx, img in enumerate(imgs):
            imgs[idx] = img.astype(dtype=np.float32, copy=True)

        for idx, anno in enumerate(annos):
            annos[idx] = anno.astype(dtype=np.float32, copy=True)

        return imgs, annos



class Stack(object):

    """
    stack adjacent frames into input tensors
    """

    def __call__(self, imgs, annos):

        num_img = len(imgs)
        num_anno = len(annos)

        h, w, = imgs[0].shape[:2]

        assert num_img == num_anno
        img_stack = np.stack(imgs, axis=0)
        anno_stack = np.stack(annos, axis=0)

        return img_stack, anno_stack

class ToTensor(object):

    """
    convert to torch.Tensor
    """

    def __call__(self, imgs, annos):

        imgs = torch.from_numpy(imgs.copy())
        annos = torch.from_numpy(annos.astype(np.uint8, copy=True)).float()

        imgs = imgs.permute(0, 3, 1, 2).contiguous()
        annos = annos.permute(0, 3, 1, 2).contiguous()

        return imgs, annos

class Normalize(object):

    def __init__(self):
        self.mean = np.array([0.485, 0.456, 0.406]).reshape([1, 1, 3]).astype(np.float32)
        self.std = np.array([0.229, 0.224, 0.225]).reshape([1, 1, 3]).astype(np.float32)

    def __call__(self, imgs, annos):

        for id, img in enumerate(imgs):
            imgs[id] = (img / 255.0 - self.mean) / self.std

        return imgs, annos

class DianJiaoTestTransform(object):

    def __init__(self):
        self.transform = Compose([
            ToFloat(),
            Normalize(),
            Stack(),
            ToTensor(),
        ])

    def __call__(self, imgs, annos):
        return self.transform(imgs, annos)
