using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using MvCamCtrl.NET;

namespace smartCheckPF.camera
{
    public delegate void DelegateShowImage(byte[] data,int length);
    public class HIKCamera
    {
        public static MyCamera.MV_CC_DEVICE_INFO_LIST m_pDeviceList = new MyCamera.MV_CC_DEVICE_INFO_LIST();

        MyCamera.cbOutputdelegate cbImage;
        //MyCamera.cbEventdelegate cbEvent;

        //定义委托对象
        public event DelegateShowImage ShowImage;

        public CAMERA m_pMyCamera;
        public CameraPara m_pCameraPara;
        public bool m_bEnabled = false;
        public bool m_bGrabbing = false;
        public bool m_bSnap = false;
        int m_nFrames = 0;      //帧数
        public uint nCamNum = 100;
        public int nIndex = -1;

        // ch:用于从驱动获取图像的缓存 | en:Buffer for getting image from driver
        UInt32 m_nBufSizeForDriver = 3072 * 2048 * 3;
        byte[] m_pBufForDriver = new byte[3072 * 2048 * 3];

        // ch:用于保存图像的缓存 | en:Buffer for saving image
        UInt32 m_nBufSizeForSaveImage = 3072 * 2048 * 3 * 3 + 2048;
        byte[] m_pBufForSaveImage = new byte[3072 * 2048 * 3 * 3 + 2048];

        public HIKCamera()
        {
            m_pMyCamera = new CAMERA();
            cbImage = new MyCamera.cbOutputdelegate(ImageCallBack1);
            m_pCameraPara = new CameraPara();
            InitHIKParaStruct();
        }

        public struct CAMERA//定义相机结构体
        {
            public MyCamera Cam_Info;
            public UInt32 m_nBufSizeForSaveImage;
            public byte[] m_pBufForSaveImage;         // 用于保存图像的缓存
        }

        //相机参数结构体
        public struct CameraPara
        {
            public float f_Exposure;
            public float f_Gain;
            public float f_FrameRate;
        }

        //初始化结构体变量参数
        public void InitHIKParaStruct()
        {
            m_pMyCamera.m_nBufSizeForSaveImage = 5472 * 3648 * 3 * 3 + 2048;
            m_pMyCamera.m_pBufForSaveImage = new byte[5472 * 3648 * 3 * 3 + 2048];
            m_pCameraPara.f_Exposure = 0;
            m_pCameraPara.f_FrameRate = 0;
            m_pCameraPara.f_Gain = 0;
        }

        // ch:枚举设备 | en:Create Device List
        public static uint DeviceListAcq()
        {
            int nRet;
            System.GC.Collect();
            nRet = MyCamera.MV_CC_EnumDevices_NET(MyCamera.MV_GIGE_DEVICE | MyCamera.MV_USB_DEVICE, ref m_pDeviceList);
            if (nRet != 0)
            {
                return 1000;
            }
            return m_pDeviceList.nDeviceNum;
        }

        // ch:显示错误信息 | en:Show error message
        private void ShowErrorMsg(string csMessage, int nErrorNum)
        {
            string errorMsg;
            if (nErrorNum == 0)
            {
                errorMsg = csMessage;
            }
            else
            {
                errorMsg = csMessage + ": Error =" + String.Format("{0:X}", nErrorNum);
            }

            switch (nErrorNum)
            {
                case MyCamera.MV_E_HANDLE: errorMsg += " Error or invalid handle "; break;
                case MyCamera.MV_E_SUPPORT: errorMsg += " Not supported function "; break;
                case MyCamera.MV_E_BUFOVER: errorMsg += " Cache is full "; break;
                case MyCamera.MV_E_CALLORDER: errorMsg += " Function calling order error "; break;
                case MyCamera.MV_E_PARAMETER: errorMsg += " Incorrect parameter "; break;
                case MyCamera.MV_E_RESOURCE: errorMsg += " Applying resource failed "; break;
                case MyCamera.MV_E_NODATA: errorMsg += " No data "; break;
                case MyCamera.MV_E_PRECONDITION: errorMsg += " Precondition error, or running environment changed "; break;
                case MyCamera.MV_E_VERSION: errorMsg += " Version mismatches "; break;
                case MyCamera.MV_E_NOENOUGH_BUF: errorMsg += " Insufficient memory "; break;
                case MyCamera.MV_E_UNKNOW: errorMsg += " Unknown error "; break;
                case MyCamera.MV_E_GC_GENERIC: errorMsg += " General error "; break;
                case MyCamera.MV_E_GC_ACCESS: errorMsg += " Node accessing condition error "; break;
                case MyCamera.MV_E_ACCESS_DENIED: errorMsg += " No permission "; break;
                case MyCamera.MV_E_BUSY: errorMsg += " Device is busy, or network disconnected "; break;
                case MyCamera.MV_E_NETER: errorMsg += " Network error "; break;
            }

            MessageBox.Show(errorMsg, "PROMPT");

        }

        //匹配自定义名称
        public int MatchCamSerialNum(string SerialNum)
        {
            if (m_pDeviceList.nDeviceNum == 0)
            {
                return -3;
            }
            else
            {
                for (int j = 0; j < m_pDeviceList.nDeviceNum; j++)
                {
                    MyCamera.MV_CC_DEVICE_INFO device = (MyCamera.MV_CC_DEVICE_INFO)Marshal.PtrToStructure(m_pDeviceList.pDeviceInfo[j], typeof(MyCamera.MV_CC_DEVICE_INFO));
                    if (device.nTLayerType == MyCamera.MV_GIGE_DEVICE)
                    {
                        IntPtr buffer = Marshal.UnsafeAddrOfPinnedArrayElement(device.SpecialInfo.stGigEInfo, 0);
                        MyCamera.MV_GIGE_DEVICE_INFO gigeInfo = (MyCamera.MV_GIGE_DEVICE_INFO)Marshal.PtrToStructure(buffer, typeof(MyCamera.MV_GIGE_DEVICE_INFO));
                        if (gigeInfo.chSerialNumber == SerialNum)
                        {
                            return j;
                        }
                        //return j;

                    }
                    else if (device.nTLayerType == MyCamera.MV_USB_DEVICE)
                    {
                        IntPtr buffer = Marshal.UnsafeAddrOfPinnedArrayElement(device.SpecialInfo.stUsb3VInfo, 0);
                        MyCamera.MV_USB3_DEVICE_INFO usbInfo = (MyCamera.MV_USB3_DEVICE_INFO)Marshal.PtrToStructure(buffer, typeof(MyCamera.MV_USB3_DEVICE_INFO));
                        //if (usbInfo.chSerialNumber == SerialNum)


                        //{
                        //    return j;
                        //}
                        return j;
                    }
                }
                return -2;
            }
        }

        //ch:打开设备 | en:Open Device
        public int InitDevice()
        {
            if (null == m_pMyCamera.Cam_Info)
            {
                m_pMyCamera.Cam_Info = new MyCamera();
                if (null == m_pMyCamera.Cam_Info)
                {
                    return -1;
                }
                return 0;
            }
            return -2;
        }

        //将列表信息的相机信息与自定义相机句柄匹配
        public int CreateDevice()
        {
            int nRet = -1;
            MyCamera.MV_CC_DEVICE_INFO device = (MyCamera.MV_CC_DEVICE_INFO)Marshal.PtrToStructure(m_pDeviceList.pDeviceInfo[nIndex], typeof(MyCamera.MV_CC_DEVICE_INFO));
            nRet = m_pMyCamera.Cam_Info.MV_CC_CreateDevice_NET(ref device);
            if (MyCamera.MV_OK != nRet)
            {
                return -1;
            }
            else
                return nRet;
        }

        //打开相机
        public int OpenDevice()
        {
            int nRet = -1;
            nRet = m_pMyCamera.Cam_Info.MV_CC_OpenDevice_NET();
            if (MyCamera.MV_OK != nRet)
            {
                return -2;
            }
            else
            {
                // ch:设置采集连续模式 | en:Set Continues Aquisition Mode
                m_pMyCamera.Cam_Info.MV_CC_SetEnumValue_NET("AcquisitionMode", 2);// ch:工作在连续模式 | en:Acquisition On Continuous Mode
                m_pMyCamera.Cam_Info.MV_CC_SetEnumValue_NET("TriggerMode", 0);
                m_pMyCamera.Cam_Info.MV_CC_SetEnumValue_NET("PixelFormat", 0x0210001F);
                m_pMyCamera.Cam_Info.MV_CC_RegisterImageCallBack_NET(cbImage, (IntPtr)0);
                m_bEnabled = true;
                return 0;
            }
        }

        public void CloseHIKDevice()
        {
            int nRet;

            nRet = m_pMyCamera.Cam_Info.MV_CC_CloseDevice_NET();
            if (MyCamera.MV_OK != nRet)
            {
                return;
            }


            nRet = m_pMyCamera.Cam_Info.MV_CC_DestroyDevice_NET();
            if (MyCamera.MV_OK != nRet)
            {
                return;
            }

            if (null != m_pMyCamera.Cam_Info)
            {
                m_pMyCamera.Cam_Info = null;
            }
        }

        //初始化相机所有操作
        public int InitHIKCamera(string SerialNum)
        {
            nIndex = MatchCamSerialNum(SerialNum);
            if (nIndex < 0)
            {
                ShowErrorMsg("枚举设备列表出错!", 0);
                return -1;
            }

            if (0 != InitDevice())
            {
                ShowErrorMsg("初始化对象出错!", 0);
                return -2;
            }

            if (0 != CreateDevice())
            {
                ShowErrorMsg("创建相机对象出错!", 0);
                return -3;
            }

            if (0 != OpenDevice())
            {
                ShowErrorMsg("打开相机对象出错!", 0);
                return -4;
            }

            //if (0 != SetGrapMode(1))
            //{
            //    ShowErrorMsg("设置触发模式出错!", 0);
            //    return -5;
            //}

            return 0;
        }

        //读取HIK相机参数
        public float GetHIKPara(string ParaName)
        {
            MyCamera.MVCC_FLOATVALUE stParam = new MyCamera.MVCC_FLOATVALUE();
            switch (ParaName)
            {
                case "ExposureTime":
                    if (MyCamera.MV_OK != m_pMyCamera.Cam_Info.MV_CC_GetFloatValue_NET("ExposureTime", ref stParam))
                    {
                        stParam.fCurValue = -1;
                    }
                    break;

                case "Gain":
                    if (MyCamera.MV_OK != m_pMyCamera.Cam_Info.MV_CC_GetFloatValue_NET("Gain", ref stParam))
                    {
                        stParam.fCurValue = -2;
                    }
                    break;

                case "ResultingFrameRate":
                    if (MyCamera.MV_OK != m_pMyCamera.Cam_Info.MV_CC_GetFloatValue_NET("ResultingFrameRate", ref stParam))
                    {
                        stParam.fCurValue = -3;
                    }
                    break;

                default:
                    stParam.fCurValue = -4;
                    break;
            }

            return stParam.fCurValue;
        }

        //读取HIK相机曝光参数
        public float GetHIKExposurePara()
        {
            return GetHIKPara("ExposureTime");
        }

        //读取HIK相机增益参数
        public float GetHIKGainPara()
        {
            return GetHIKPara("Gain");
        }

        //读取HIK相机帧率参数
        public float GetHIKFrameRatePara()
        {
            return GetHIKPara("ResultingFrameRate");
        }

        //读取HIK相机参数
        public void WriteHIKPara(CameraPara s_para)
        {
            int nRet;
            m_pMyCamera.Cam_Info.MV_CC_SetEnumValue_NET("ExposureAuto", 0);
            nRet = m_pMyCamera.Cam_Info.MV_CC_SetFloatValue_NET("ExposureTime", s_para.f_Exposure);
            if (nRet != MyCamera.MV_OK)
            {
                ShowErrorMsg("Set Exposure Time Fail!", nRet);
            }

            m_pMyCamera.Cam_Info.MV_CC_SetEnumValue_NET("GainAuto", 0);
            nRet = m_pMyCamera.Cam_Info.MV_CC_SetFloatValue_NET("Gain", s_para.f_Gain);
            if (nRet != MyCamera.MV_OK)
            {
                ShowErrorMsg("Set Gain Fail!", nRet);
            }

            //nRet = m_pMyCamera.Cam_Info.MV_CC_SetFloatValue_NET("AcquisitionFrameRate", s_para.f_FrameRate);
            //if (nRet != MyCamera.MV_OK)
            //{
            //    ShowErrorMsg("Set Frame Rate Fail!", nRet);
            //}
        }

        //设置取图模式 0-连续模式 1触发模式
        public int SetGrapMode(uint Index)
        {
            return m_pMyCamera.Cam_Info.MV_CC_SetEnumValue_NET("TriggerMode", Index);
        }

        //设置触发源 7-软触发
        public int SetTriggerSource(uint Index)
        {
            // ch:触发源选择:0 - Line0; | en:Trigger source select:0 - Line0;
            //           1 - Line1;
            //           2 - Line2;
            //           3 - Line3;
            //           4 - Counter;
            //           7 - Software;
            return m_pMyCamera.Cam_Info.MV_CC_SetEnumValue_NET("TriggerSource", Index);
        }

        //开始取流函数
        public int OpenGrap(bool TF)
        {
            int nRet;
            if (TF)
            {
                // ch:开始采集 | en:Start Grabbing
                nRet = m_pMyCamera.Cam_Info.MV_CC_StartGrabbing_NET();
                if (MyCamera.MV_OK != nRet)
                {
                    ShowErrorMsg("Trigger Fail!", nRet);
                    return -1;
                }
                m_bGrabbing = true;
            }
            else
            {
                nRet = m_pMyCamera.Cam_Info.MV_CC_StopGrabbing_NET();
                if (MyCamera.MV_OK != nRet)
                {
                    ShowErrorMsg("Trigger Fail!", nRet);
                    return -2;
                }
                m_bGrabbing = false;
            }
            return nRet;
        }

        //设置白平衡  gamma值等
        public void  SetStaticCameraPara()
        {
            //Enable Gamma as sRGB
            m_pMyCamera.Cam_Info.MV_CC_SetEnumValue_NET("GammaSelector",2);
            m_pMyCamera.Cam_Info.MV_CC_SetBoolValue_NET("GammaEnable", true);

            //Set AWB as off  强制白平衡
            m_pMyCamera.Cam_Info.MV_CC_SetEnumValue_NET("BalanceWhiteAuto", 0);
        }

        //获取一张图片到成员变量中
        public int GetOneImage()
        {
            m_bSnap = false;
            if (m_bGrabbing)
            {
                m_bSnap = true;
                return 0;
            }
            else
                return -1;
        }


        // ch:取流回调函数 | en:Aquisition Callback Function
        private void ImageCallBack1(IntPtr pData, ref MyCamera.MV_FRAME_OUT_INFO pFrameInfo, IntPtr pUser)
        {
            int nRet;
            int nIndex = (int)pUser;
            ++m_nFrames;

            UInt32 nPayloadSize = 0;
            MyCamera.MVCC_INTVALUE stParam = new MyCamera.MVCC_INTVALUE();
            nRet = m_pMyCamera.Cam_Info.MV_CC_GetIntValue_NET("PayloadSize", ref stParam);
            if (MyCamera.MV_OK != nRet)
            {
                return;
            }
            nPayloadSize = stParam.nCurValue;
            if (nPayloadSize > m_nBufSizeForDriver)
            {
                m_nBufSizeForDriver = nPayloadSize;
                m_pBufForDriver = new byte[m_nBufSizeForDriver];

                // ch:同时对保存图像的缓存做大小判断处理 | en:Determine the buffer size to save image
                // ch:BMP图片大小：width * height * 3 + 2048(预留BMP头大小) | en:BMP image size: width * height * 3 + 2048 (Reserved for BMP header)
                m_nBufSizeForSaveImage = m_nBufSizeForDriver * 3 + 2048;
                m_pBufForSaveImage = new byte[m_nBufSizeForSaveImage];
            }

            if (m_bSnap)
            {
                IntPtr pImage = Marshal.UnsafeAddrOfPinnedArrayElement(m_pBufForSaveImage, 0);
                MyCamera.MV_SAVE_IMAGE_PARAM_EX stSaveParam = new MyCamera.MV_SAVE_IMAGE_PARAM_EX();
                stSaveParam.enImageType = MyCamera.MV_SAVE_IAMGE_TYPE.MV_Image_Jpeg;
                //stSaveParam.enImageType = MyCamera.MV_SAVE_IAMGE_TYPE.MV_Image_Bmp;
                stSaveParam.enPixelType = pFrameInfo.enPixelType;
                stSaveParam.pData = pData;
                stSaveParam.nDataLen = pFrameInfo.nFrameLen;
                stSaveParam.nHeight = pFrameInfo.nHeight;
                stSaveParam.nWidth = pFrameInfo.nWidth;
                stSaveParam.pImageBuffer = pImage;
                stSaveParam.nBufferSize = m_nBufSizeForSaveImage;
                stSaveParam.nJpgQuality = 99;
                nRet = m_pMyCamera.Cam_Info.MV_CC_SaveImageEx_NET(ref stSaveParam);
                if (MyCamera.MV_OK != nRet)
                {
                    return;
                }
                if (ShowImage != null)
                {
                    ShowImage(m_pBufForSaveImage, (int)stSaveParam.nImageLen);
                }
                m_bSnap = false;
            }


    }






    }
}
