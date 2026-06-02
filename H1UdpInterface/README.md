# H1 UDP/WebSocket 接口库（MuJoCo接口兼容）

本库基于WebSocket（ws）与Unity端通信，完全兼容MuJoCo风格动作与观测API，极易迁移。

## 特性
- 无需硬编码任何客户端地址，每个Python会话自动连接、自动收发、支持多客户端/多实例
- Unity端只需挂载WebSocket_H1Controller配置端口，Python只写Unity IP即可
- API 兼容mujoco RL用法，代码级零迁移

## 安装方法

```sh
cd H1UdpInterface
pip install . 
python -m pip install websockets  # WebSocket依赖
```

## 快速上手
```python
from h1_udp.h1client import H1UdpEnv
import numpy as np

env = H1UdpEnv(server_ip='127.0.0.1', port=9001, path='/ws/')
obs = env.reset()
for _ in range(100):
    action = np.zeros(18)
    obs, r, done, info = env.step(action)
env.close()
```

**Unity端需：**
- 在`WebSocketBaseCommunicator`中设定监听地址/端口/路径（默认 `http://localhost:9001/ws/`）
- Python端 `H1UdpEnv` 中 `path` 参数需与Unity端保持一致（默认为 `/ws/`）

## API 与 MuJoCo 差异点
- action、observation结构一致，全部通过单一ws连接，支持多会话
- reset自动拉取观测，无需reset命令
- 观测与回传完全自动匹配，不丢包

## 与Unity/自定义机器人通信
- Unity采用WebSocketBaseCommunicator+WebSocket_H1Controller即可支持任意机器人拓展
- 无需对字段做任何远端映射，只需保持双方json格式一致

## 问题反馈
如遇连接、性能、兼容等需求请随时反馈。
