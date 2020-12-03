# 历史版本修改记录

* 20120/11/11　　　　`v1.3`

    1. 修改配置文件参数。

* 20120/11/05　　　　`v1.2`

    1. 修改配置文件。

* 20120/10/15　　　　`v1.1`

    1. 修复BUG。
    2. 加入YOLO。

* 20120/10/14　　　　`v1.0`
    
    1. 李时珍模块化初始版本。只包含服务器端预测函数功能。



### CenterNet
MobileNet v3 + CenterNet

### SVM
hog， lbp， rgb_hist， otsu等。

### CheckSimilar
similar = 0.31\*sift  + 0.25\*rgb_hist + 0.31\*hog + 0.13\*lbp


# environment

1. python3.7 以上
2. opencv 3.4.2.16
3. opencv_contrib 3.4.2.16
4. pytorch 1.0以上
5. scikit-learn 0.21.2
6. scipy 1.3.0

# Reference
* [CenterNet](https://github.com/xingyizhou/CenterNet "CenterNet")
* [MobileNet v3](https://github.com/xiaolai-sqlai/mobilenetv3 "MobileNet v3")
* [DBFace](https://github.com/dlunion/DBFace "DBFace")

# Author
* [liuwei1023](https://github.com/liuwei1023 "liuwei1023")