using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WindowsFormsApp1
{

    public static class 账号密码
    {
        public static string 账号 { get; set; }
        public static string 密码 { set; get; }
    }
  public static class 数据变量
    {
        public static string 相机1曝光时间 {  get; set; }
        public static string 相机1增益 { get; set; }
        public static string 相机2曝光时间 {  get; set; }
        public static string 相机2增益 { get; set; }
        public static string 相机3曝光时间 { get; set; }
        public static string 相机3增益 { get; set; }
        public static string 相机4曝光时间 { get; set; }
        public static string 相机4增益 { get; set; }

        public static string 相机1模板数量 { set; get; }
        public static string 相机1对比度 { set; get; }
        public static string 相机1匹配度 { set; get; }
        public static string 相机2模板数量 { set; get; }
        public static string 相机2对比度 { set; get; }
        public static string 相机2匹配度 { set; get; }
        public static string 相机3模板数量 { set; get; }
        public static string 相机3对比度 { set; get; }
        public static string 相机3匹配度 { set; get; }
        public static string 相机4模板数量 { set; get; }
        public static string 相机4对比度 { set; get; }
        public static string 相机4匹配度 { set; get; }

        public static string 相机1原图保存路径 { set; get; }
        public static string 相机1OK图保存路径 {  set; get; }
        public static string 相机1NG图保存路径 {  set; get; }
        public static string 相机2原图保存路径 { set; get; }
        public static string 相机2OK图保存路径 {  set; get; }
        public static string 相机2NG图保存路径 {  set; get; }
        public static string 相机3原图保存路径 { set; get; }
        public static string 相机3OK图保存路径 {  set; get; }
        public static string 相机3NG图保存路径 {  set; get; }
        public static string 相机4原图保存路径 { set; get; }
        public static string 相机4OK图保存路径 { set; get; }
        public static string 相机4NG图保存路径 { set; get; }

        public static string 相机1hdl文件路径 {  set; get; }
        public static string 相机1hdict文件路径 {  set; get; }
        public static string 相机2hdl文件路径 { set; get; }
        public static string 相机2hdict文件路径 { set; get; }
        public static string 相机3hdl文件路径 { set; get; }
        public static string 相机3hdict文件路径 { set; get; }
        public static string 相机4hdl文件路径 { set; get; }
        public static string 相机4hdict文件路径 { set; get; }

      public static string 相机1模板保存路径 {  set; get; }
        public static string 相机2模板保存路径 { set; get;}
        public static string 相机3模板保存路径 { set; get; }
        public static string 相机4模板保存路径 { set; get; }

        public static string 料号名称 {  set; get; }
    
        public static string 权限时间 {  set; get; }
        public static string 图片删除日期 { get; set; }
        public static string 相机数量设置 { get; set; }

    }
    public static class 状态
    {
        public static bool 登录权限 = false;
        public static bool 选择区域时halcon不可动 = true;
        public static bool 控制放大缩小后图片的显示 = false;
        public static bool 自动状态 = false;
        public static bool 实时状态 = false;
        
        }
       
        
    
    public static class 相机变量
    {
        public static List<MVS变量> CameraList = new List<MVS变量>();
        public static List<string>料号集合= new List<string>();
    }
    public static class 只显示
    {
        public static string 相机1曝光时间 { get; set; }
        public static string 相机1增益 { get; set; }
        public static string 相机2曝光时间 { get; set; }
        public static string 相机2增益 { get; set; }
        public static string 相机3曝光时间 { get; set; }
        public static string 相机3增益 { get; set; }
        public static string 相机4曝光时间 { get; set; }
        public static string 相机4增益 { get; set; }

        public static string 相机1模板数量 { set; get; }
        public static string 相机1对比度 { set; get; }
        public static string 相机1匹配度 { set; get; }
        public static string 相机2模板数量 { set; get; }
        public static string 相机2对比度 { set; get; }
        public static string 相机2匹配度 { set; get; }
        public static string 相机3模板数量 { set; get; }
        public static string 相机3对比度 { set; get; }
        public static string 相机3匹配度 { set; get; }
        public static string 相机4模板数量 { set; get; }
        public static string 相机4对比度 { set; get; }
        public static string 相机4匹配度 { set; get; }

        public static string 相机1原图保存路径 { set; get; }
        public static string 相机1OK图保存路径 { set; get; }
        public static string 相机1NG图保存路径 { set; get; }
        public static string 相机2原图保存路径 { set; get; }
        public static string 相机2OK图保存路径 { set; get; }
        public static string 相机2NG图保存路径 { set; get; }
        public static string 相机3原图保存路径 { set; get; }
        public static string 相机3OK图保存路径 { set; get; }
        public static string 相机3NG图保存路径 { set; get; }
        public static string 相机4原图保存路径 { set; get; }
        public static string 相机4OK图保存路径 { set; get; }
        public static string 相机4NG图保存路径 { set; get; }

        public static string 相机1hdl文件路径 { set; get; }
        public static string 相机1hdict文件路径 { set; get; }
        public static string 相机2hdl文件路径 { set; get; }
        public static string 相机2hdict文件路径 { set; get; }
        public static string 相机3hdl文件路径 { set; get; }
        public static string 相机3hdict文件路径 { set; get; }
        public static string 相机4hdl文件路径 { set; get; }
        public static string 相机4hdict文件路径 { set; get; }

        public static string 相机1模板保存路径 { set; get; }
        public static string 相机2模板保存路径 { set; get; }
        public static string 相机3模板保存路径 { set; get; }
        public static string 相机4模板保存路径 { set; get; }

        public static string 料号名称 { set; get; }

        public static string 权限时间 { set; get; }
        public static string 图片删除日期 { get; set; }
        public static string 相机数量设置 { get; set; }

    }
}
