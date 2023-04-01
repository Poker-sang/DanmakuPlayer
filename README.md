# DanmakuPlayer

## 介绍

* 支持 [bilibili](bilibili.com) xml格式和protobuf直接下载的弹幕文件的透明弹幕播放器

* UI使用WinUI3框架，上个WPF版本 [链接](https://github.com/Poker-sang/DanmakuPlayerWpf)

* 获取弹幕依赖 [protobuf-net](https://github.com/protobuf-net/protobuf-net) 库

* 弹幕渲染使用 [vortice](https://github.com/amerkoleci/vortice) 的DirectX库
 
* 龟速更新中

## 预览

B站视频[【炮姐/AMV】我永远都会守护在你的身边！](https://www.bilibili.com/video/BV1Js411o76u)

### 完整弹幕

![full](https://github.com/Poker-sang/DanmakuPlayer/blob/master/readme/full.png)

### 合并类似弹幕、同屏不重叠

![conbined](https://github.com/Poker-sang/DanmakuPlayer/blob/master/readme/conbined.png)

## 使用说明

⚠️：指实现比较困难的功能

### 界面

* [x] 调整透明度

* [x] 固定最上层

* [x] 改变主题色

### 弹幕文件

* [x] 从本地打开（.xml 类型）

* [x] 用bilibili API通过av、BV、cid、md、ss、ep等下载

* [x] 分P获取弹幕

* [x] 获取全弹幕

### 播放

* [x] 调整快进速度

* [x] 调整播放倍速

* [x] 暂停、快进等快捷键

* [x] 播放时允许调整窗口大小

* [x] 播放时调整设置

* [x] 输入进度条

* [ ] ⚠️ 和背后播放器同步

### 弹幕

* [x] 顶端、底端、滚动、逆向、彩色弹幕

* [x] 调整透明度

* [x] 调整滚动速度

* [x] 出现位置算法优化

* [x] 弹幕不重叠

* [x] 大小弹幕

* [x] 弹幕字体

* [x] 合并类似弹幕

* [x] 大小弹幕出现位置优化

* [x] 再次优化弹幕出现算法

* [x] 正则屏蔽弹幕

* [ ] 同屏最多（顶端、底端、滚动）弹幕限制

* [ ] 弹幕阴影、描边等效果

* [ ] ⚠️ 高级弹幕

### 其他

* [x] 弹幕多时流畅度

* [x] 优化项目结构

* [ ] ⚠️ 正则高亮与错误提示

* [ ] ⚠️ 分段加载弹幕以降低占用

* [ ] ⚠️ 降低内存占用

* [ ] 其他常用功能...（没考虑到的x）

## 关于项目

项目名称：DanmakuPlayer

项目地址：[GitHub](https://github.com/Poker-sang/DanmakuPlayer)

版本：3.10

## 联系方式

作者：[扑克](https://github.com/Poker-sang)

邮箱：poker_sang@outlook.com

QQ：[2639914082](http://wpa.qq.com/msgrd?v=3&uin=2639914082&site=qq&menu=yes)

2022.11.22
