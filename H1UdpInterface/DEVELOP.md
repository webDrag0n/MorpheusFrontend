# H1UdpInterface 开发与发布指南

## 目录结构说明

```text
H1UdpInterface/
│
├─ h1_udp/              # 主要库源码
│   ├─ __init__.py
│   └─ h1client.py      # UDP客户端实现（兼容MuJoCo风格）
│
├─ examples/            # 用法演示脚本
│   └─ run_h1_udp_demo.py
│
├─ README.md            # 用户入门指引（推荐先看）
├─ DEVELOP.md           # 本文档：开发&发布指引
├─ setup.py             # pip包配置
```

## 如何本地开发&测试

1. 保持在`H1UdpInterface`目录下，修改源码、用`examples/run_h1_udp_demo.py`进行调试。
2. 可用`python -m pip install -e .`实现本地开发模式。修改后无需重装即可直接导入测试。

## 如何发布到PyPI

1. 安装构建/publish工具：
   ```sh
   pip install build twine
   ```
2. 构建与分发：
   ```sh
   python -m build
   twine upload dist/*
   ```
3. 每次发布前建议 bump 版本号（setup.py/pyproject.toml，如果有）。

## 协议字段说明与自定义

- 推荐使用JSON传输格式，字段顺序与Unity端严格保持一致即可。
- H1ControlCommand格式可参考`h1client.py`中的key顺序（与C#字段完全对应）。
- 如需扩展状态/动作字段，仅需双方一致即可。

## 兼容其他机器人

- 仅需在C#与python代码间同步字段名与数量（如`action_space`/`observation_space`长度及key）。
- UDP通信实现完全解耦，协议定制极其灵活。

## 协议调试技巧

- 建议用wireshark/tcpdump抓包分析，或直接调试log，定位丢包/字段对不齐等问题。
- Python端遇到json解析失败大多为格式/顺序不一致。
