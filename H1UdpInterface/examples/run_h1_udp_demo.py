from h1_udp.h1client import H1UdpEnv
import numpy as np

# pip install websockets 用于WebSocket通信
# Unity端需挂载WebSocket_H1Controller并设置同端口

env = H1UdpEnv(server_ip='127.0.0.1', port=9001, path='/ws/')
obs = env.reset()
print('初始观测:', obs)
for i in range(100):
    action = np.random.uniform(-1, 1, 18)
    obs, reward, done, info = env.step(action)
    print(f'步{i}，观测:{obs}')
    if done:
        break
env.close()
