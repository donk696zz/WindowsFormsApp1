using MvCamCtrl.NET;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;




namespace WindowsFormsApp1
{
    // 你自己定义的类：MVS变量
    // 所有相机的 变量、方法、连接、IO触发 全部在这里
    public class MVS变量
    {
        #region 相机核心变量
        // 海康相机SDK实例对象
        public MyCamera Cam;

        // 相机编号 0=相机1 / 1=相机2 / 2=相机3 / 3=相机4
        public int 相机编号;

        // 相机是否已经打开
        public bool 是否已打开;

        // 相机是否正在采集图像
        public bool 是否正在采集;
        #endregion

        #region 线程取图变量（官方Demo标准写法）
        // 图像接收线程
        private Thread 取图线程;

        // 线程运行标记（控制线程开始/停止）
        private volatile bool 线程运行;
        private readonly object 采集状态锁 = new object();
        private readonly object 输出信号锁 = new object();
        #endregion

        #region 图像内存缓存变量
        // 图像数据内存指针
        public IntPtr 图像缓存;

        // 图像缓存大小
        public uint 缓存大小;

        // 图像信息（宽度、高度、像素格式等）
        public MyCamera.MV_FRAME_OUT_INFO_EX 帧信息;

        // 线程锁：防止多线程同时操作内存导致崩溃
        private readonly object 内存锁 = new object();
        #endregion

        #region 图像回调（外部接收图像：显示+检测）
        // 图像到达委托（给主界面/halcon使用）
        public delegate void 图像委托(IntPtr pData, ref MyCamera.MV_FRAME_OUT_INFO_EX info, int index);

        // 图像到达事件（触发一次，就抛一张图出去）
        public event 图像委托 图像已到达;
        #endregion
        #region 新增：实时预览委托（一直执行，纯实时流）
        /// <summary>
        /// 实时预览开关
        /// </summary>
        public bool 实时预览开启 = false;

        /// <summary>
        /// 实时流图像委托
        /// </summary>
        public delegate void 实时预览委托(IntPtr pData, ref MyCamera.MV_FRAME_OUT_INFO_EX info, int index);

        /// <summary>
        /// 实时流图像到达事件
        /// </summary>
        public event 实时预览委托 实时图像到达;
        #endregion





        #region 系统API：内存拷贝（和官方Demo完全一致）
        [DllImport("kernel32.dll", EntryPoint = "CopyMemory", SetLastError = false)]
        public static extern void CopyMemory(IntPtr dest, IntPtr src, uint count);
        #endregion

        /// <summary>
        /// 构造函数：初始化相机对象
        /// </summary>
        /// <param name="index">相机编号</param>
        public MVS变量(int index)
        {
            相机编号 = index;             // 赋值编号
            Cam = new MyCamera();         // 创建相机实例
            图像缓存 = IntPtr.Zero;        // 初始内存为空
            帧信息 = new MyCamera.MV_FRAME_OUT_INFO_EX(); // 初始化图像信息
        }

        /// <summary>
        /// 打开相机（官方标准流程）
        /// </summary>
        /// <param name="设备信息">相机枚举到的设备信息</param>
        /// <returns>0=成功</returns>
        public int 打开相机(ref MyCamera.MV_CC_DEVICE_INFO 设备信息)
        {
            // 1. 创建相机设备
            int nRet = Cam.MV_CC_CreateDevice_NET(ref 设备信息);
            if (nRet != 0) return nRet;

            // 2. 打开相机
            nRet = Cam.MV_CC_OpenDevice_NET();
            if (nRet != 0)
            {
                Cam.MV_CC_DestroyDevice_NET(); // 打开失败，销毁设备
                return nRet;
            }

            // 3. GigE相机网口优化（自动设置最佳包大小，必须加）
            if (设备信息.nTLayerType == MyCamera.MV_GIGE_DEVICE)
            {
                int packSize = Cam.MV_CC_GetOptimalPacketSize_NET();
                if (packSize > 0)
                {
                    Cam.MV_CC_SetIntValue_NET("GevSCPSPacketSize", (uint)packSize);
                }
            }

            // 标记相机已打开
            是否已打开 = true;
            return 0;
        }

        /// <summary>
        /// 从全局静态类【数据变量】加载曝光、增益
        /// </summary>
        /// <returns>0=成功</returns>
        public int 设置曝光增益()
        {
            try
            {
                int nRet = 0;
                double 曝光 = 10000;
                double 增益 = 10;

                // 根据相机编号自动匹配参数
                switch (相机编号)
                {
                    case 0: 曝光 = double.Parse(数据变量.相机1曝光时间); 增益 = double.Parse(数据变量.相机1增益); break;
                    case 1: 曝光 = double.Parse(数据变量.相机2曝光时间); 增益 = double.Parse(数据变量.相机2增益); break;
                    case 2: 曝光 = double.Parse(数据变量.相机3曝光时间); 增益 = double.Parse(数据变量.相机3增益); break;
                    case 3: 曝光 = double.Parse(数据变量.相机4曝光时间); 增益 = double.Parse(数据变量.相机4增益); break;
                }

                // 关闭自动曝光 → 设置手动曝光
                nRet |= Cam.MV_CC_SetEnumValue_NET("ExposureAuto", 0);
                nRet |= Cam.MV_CC_SetFloatValue_NET("ExposureTime", (float)曝光);

                // 关闭自动增益 → 设置手动增益
                nRet |= Cam.MV_CC_SetEnumValue_NET("GainAuto", 0);
                nRet |= Cam.MV_CC_SetFloatValue_NET("Gain", (float)增益);

                return nRet;
            }
            catch
            {
                // 参数异常返回错误
                return -1;
            }
        }
        /// <summary>
        /// 【实时生效】直接设置曝光和增益（采集过程中也能改）
        /// </summary>
        public void 实时设置曝光增益(double exp, double gain)
        {
            if (Cam == null) return;

            // 1. 必须关闭自动曝光、自动增益（重中之重）
            Cam.MV_CC_SetEnumValue_NET("ExposureAuto", 0);
            Cam.MV_CC_SetEnumValue_NET("GainAuto", 0);

            // 2. 直接SDK下发硬件参数
            Cam.MV_CC_SetFloatValue_NET("ExposureTime", (float)exp);
            Cam.MV_CC_SetFloatValue_NET("Gain", (float)gain);
        }

        /// <summary>
        /// 【核心】设置PLC硬触发（随机触发专用）
        /// 触发源：Line0（相机DI0）
        /// 触发方式：上升沿触发
        /// 触发规则：来一次信号，拍一张图
        /// </summary>
        public int 设置硬触发()
        {
            int nRet = 0;

            // 1. 采集模式 = 连续采集 (必须是2！)
            nRet |= Cam.MV_CC_SetEnumValue_NET("AcquisitionMode", 2);

            // 2. 触发模式 = 开启
            nRet |= Cam.MV_CC_SetEnumValue_NET("TriggerMode", 1);
            // ✅【你漏了这句！不加永远不触发】
            nRet |= Cam.MV_CC_SetEnumValue_NET("TriggerSelector", 0);

            // 3. 触发源 = Line0 （PLC接这里）
            nRet |= Cam.MV_CC_SetEnumValue_NET("TriggerSource", 0);

            // 4. 触发沿 = 上升沿
            nRet |= Cam.MV_CC_SetEnumValue_NET("TriggerActivation", 0);

            // 5. ✅【必须加！】触发重叠 = 允许 （无限触发关键）
            nRet |= Cam.MV_CC_SetEnumValue_NET("TriggerOverlap", 1);

            // 6. 每次硬件触发只采集一帧，保证一次触发对应一次检测结果
            nRet |= Cam.MV_CC_SetIntValue_NET("AcquisitionBurstFrameCount", 1);

            return nRet;
        }
        /// <summary>
        /// 【关键】设置连续采集模式（实时预览用）
        /// </summary>
        public void 设置连续采集模式()
        {
            // 关闭触发 → 实时流
            Cam.MV_CC_SetEnumValue_NET("TriggerMode", 0);
            // 【必须】关闭自动曝光/自动增益，否则改不了
            Cam.MV_CC_SetEnumValue_NET("ExposureAuto", 0);
            Cam.MV_CC_SetEnumValue_NET("GainAuto", 0);
        }
        /// <summary>
        /// 开始采集（启动线程，进入等待PLC触发状态）
        /// </summary>
        /// <returns>0=成功</returns>
        public int 开始采集(bool 使用硬触发 = true)
        {
            lock (采集状态锁)
            {
                if (!是否已打开 || Cam == null)
                    return -1;

                if (是否正在采集)
                    return 0;

                if (使用硬触发)
                {
                    int triggerResult = 设置硬触发();
                    if (triggerResult != 0)
                        return triggerResult;
                }

                // 先让SDK进入采集状态，再启动取图线程，避免线程访问未启动的设备。
                int nRet = Cam.MV_CC_StartGrabbing_NET();
                if (nRet != 0)
                    return nRet;

                线程运行 = true;
                是否正在采集 = true;
                取图线程 = new Thread(取图循环)
                {
                    IsBackground = true,
                    Name = $"相机{相机编号 + 1}取图线程"
                };
                取图线程.Start();
                return nRet;
            }
        }

        /// <summary>
        /// 【核心】取图线程（永久等待PLC触发）
        /// 逻辑：PLC不来信号 → 一直等待
        ///      PLC来一次信号 → 收到一张图 → 抛出去显示+检测
        /// </summary>
        private void 取图循环()
        {
            // 定义图像帧结构体
            MyCamera.MV_FRAME_OUT 帧 = new MyCamera.MV_FRAME_OUT();

            // 线程循环
            while (线程运行)
            {
                // 阻塞等待图像，超时1秒
                int nRet = Cam.MV_CC_GetImageBuffer_NET(ref 帧, 1000);

                // 收到图像
                if (nRet == 0)
                {
                    try
                    {
                        // 加锁，安全操作内存
                        lock (内存锁)
                        {
                            // 如果缓存为空或图像变大 → 重新分配内存
                            if (图像缓存 == IntPtr.Zero || 帧.stFrameInfo.nFrameLen > 缓存大小)
                            {
                                if (图像缓存 != IntPtr.Zero)
                                    Marshal.FreeHGlobal(图像缓存); // 释放旧内存

                                // 分配新内存
                                图像缓存 = Marshal.AllocHGlobal((int)帧.stFrameInfo.nFrameLen);
                                缓存大小 = 帧.stFrameInfo.nFrameLen;
                            }

                            // 保存图像信息
                            帧信息 = 帧.stFrameInfo;

                            // 复制图像数据到缓存
                            CopyMemory(图像缓存, 帧.pBufAddr, 帧.stFrameInfo.nFrameLen);
                        }

                        // 图像抛出 → 给Halcon显示 + 视觉检测
                        图像已到达?.Invoke(图像缓存, ref 帧信息, 相机编号);

                        // 实时窗体图像（不受自动检测开关影响）
                        if (实时预览开启)
                        {
                            实时图像到达?.Invoke(图像缓存, ref 帧信息, 相机编号);
                        }
                    }
                    finally
                    {
                        // 无论检测回调是否异常，都必须归还SDK图像缓存。
                        Cam.MV_CC_FreeImageBuffer_NET(ref 帧);
                    }
                }
                else
                {
                    // 无触发信号 → 休眠5ms，降低CPU占用
                    Thread.Sleep(5);
                }
            }
        }

        /// <summary>
        /// 停止采集
        /// </summary>
        public void 停止采集()
        {
            Thread threadToJoin;
            lock (采集状态锁)
            {
                if (!是否正在采集)
                    return;

                线程运行 = false;
                是否正在采集 = false;
                threadToJoin = 取图线程;
            }

            // 先停止SDK以解除阻塞取图，再等待线程退出。
            Cam?.MV_CC_StopGrabbing_NET();
            if (threadToJoin != null && threadToJoin != Thread.CurrentThread)
                threadToJoin.Join();

            lock (采集状态锁)
            {
                取图线程 = null;
            }
        }

        /// <summary>
        /// 关闭相机并释放所有资源
        /// </summary>
        public void 关闭相机()
        {
            if (Cam == null)
                return;

            // 先停止采集
            停止采集();

            // 释放图像内存
            if (图像缓存 != IntPtr.Zero)
            {
                Marshal.FreeHGlobal(图像缓存);
                图像缓存 = IntPtr.Zero;
            }

            // 关闭相机 + 销毁设备
            if (是否已打开)
            {
                Cam.MV_CC_CloseDevice_NET();
                Cam.MV_CC_DestroyDevice_NET();
            }
            是否已打开 = false;
        }

        #region 相机IO输出（给PLC的OK/NG信号，已修复报错）
        /// <summary>
        /// 输出OK信号 → 相机Line2输出200ms高电平脉冲
        /// </summary>
        public int 输出OK()
        {
            return 输出脉冲(2);
        }

        /// <summary>
        /// 输出NG信号 → 相机Line3输出200ms高电平脉冲
        /// </summary>
        public int 输出NG()
        {
            return 输出脉冲(3);
        }

        /// <summary>
        /// 将一次视觉判定转换为一个确定的PLC脉冲。
        /// </summary>
        public int 输出检测结果(bool 结果OK)
        {
            return 结果OK ? 输出OK() : 输出NG();
        }

        private int 输出脉冲(uint 线号)
        {
            if (!是否已打开 || Cam == null)
                return -1;

            lock (输出信号锁)
            {
                int nRet = 0;
                nRet |= Cam.MV_CC_SetEnumValue_NET("LineSelector", 线号);
                nRet |= Cam.MV_CC_SetEnumValue_NET("LineMode", 1);
                nRet |= Cam.MV_CC_SetEnumValue_NET("LineStatus", 1);
                if (nRet != 0)
                    return nRet;

                try
                {
                    Thread.Sleep(200);
                }
                finally
                {
                    nRet |= Cam.MV_CC_SetEnumValue_NET("LineStatus", 0);
                }

                return nRet;
            }
        }
        #endregion
    }
}
