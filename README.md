# Morpheus

## Introduction

**本项目的目标是开发一个专注于人-机协同任务的机器人仿真环境**，集成ROS（Robot Operating System）、Unity和MuJoCo的仿真平台，具备高精度的物理仿真、灵活的环境建模、传感器模拟以及基于大语言模型的AI动作绑定和人机协作。通过容器化简化部署，并集成Hololens 2进行场景可视化和人体动作捕捉。该平台还支持通过照片生成场景并扩展类似场景，以及优化人机协作的数据集生成流程。

![Morpheus愿景](README.assets/Morpheus愿景.png)

![Morpheus关键组件](README.assets/Morpheus关键组件.png)

## How to Run

Clone this repository and open with unity 2022 LTS.

Then deploy Morpheus Backend according to backend README:

[Morpheus Backend GitHub Repo](https://github.com/webDrag0n/MorpheusBackend)

## Status

- ## Roadmap
	- ✅ MuJoCo
		- Unity端插件部署完成
	- ✅ [[Unitree MuJoCo]]
		- 部署完成
	- ✅ [[Unitree sdk2]] [[Unitree sdk2 python]]
	- ✅ ML-Agent
	- ✅ ROS Plugin：Unity-Robotics-Hub
		- ROS2（foxy）与Unity通信完成测试
	- ▶️ Isaac Sim RL Sim2Sim测试
		- ✅ 环境部分部署完成
		- ✅ 仿真环境机器人控制指令接收
		- ▶️ 仿真环境机器人状态回传
			- ROS2控制信号接收，状态信号发送
	- ▶️ Hololens 2 连接Unity
		- ✅ Microsoft-MRTK3.0 OpenXR技术栈部署完成
		- ✅ Hololens 2连接Unity
		- ⏸️ Hololens 2手部动捕信号回传
		- ⏸️ Hololens 2相机信号回传
	- ⏸️ Hololens 2手部输入反控仿真物体
		- ⏸️ 手部及位置动捕数据
			- ⏸️ 接收模块
			- ⏸️ 录制模块
		- ⏸️ Unity接收动捕+图像回传数据对齐
	- ⏸️ 仿真数据录制模块
		- Unitree H1
		- Unitree Go2
		- 四旋翼无人机
	- ⏸️ Robomaster机器人MuJoCo模型
	- ⏸️ Unity输出语义分割图
		- ⏸️ SAM2？或者直接仿真直出
	- ⏸️ 传感器仿真
		- ⏸️ 相机（自然有，只需要接口）
		- ⏸️ 激光雷达
		- ⏸️ IMU（简单，只需要接口）


## Contributors

@[webDrag0n](https://github.com/webDrag0n), @[Tsunami](https://github.com/panz1ha0)
