import asyncio
import json
import numpy as np
import websockets

class H1UdpEnv:
    """
    使用WebSocket协议与Unity H1 WebSocket服务端通信。
    数据协议、API与MuJoCo风格一致，零迁移。
    """
    def __init__(self, server_ip='127.0.0.1', port=9001, path='/ws/'):
        self.server_ip = server_ip
        self.port = port
        path = path if path.startswith('/') else '/' + path
        if not path.endswith('/'):
            path += '/'
        self.uri = f"ws://{server_ip}:{port}{path}"
        self.loop = asyncio.get_event_loop()
        self.websocket = self.loop.run_until_complete(websockets.connect(self.uri))
        # action/obs空间API —— 兼容MuJoCo RL范式
        self.action_space = lambda: np.zeros(18, dtype=np.float32)
        self.observation_space = lambda: np.zeros(18, dtype=np.float32)

    def reset(self):
        # 直接取到首次观测即可，无需reset cmd
        return self.step(self.action_space())[0]

    def step(self, action):
        if isinstance(action, dict):
            cmd = action
        else:
            keys = [
                'left_hip_yaw', 'left_hip_roll', 'left_hip_pitch', 'left_knee', 'left_ankle',
                'right_hip_yaw', 'right_hip_roll', 'right_hip_pitch', 'right_knee', 'right_ankle',
                'torso',
                'left_shoulder_pitch', 'left_shoulder_roll', 'left_shoulder_yaw', 'left_elbow',
                'right_shoulder_pitch', 'right_shoulder_roll', 'right_shoulder_yaw', 'right_elbow']
            cmd = {k: float(v) for k, v in zip(keys, action)}
        msg = json.dumps(cmd)
        # 发送动作，等待观测
        self.loop.run_until_complete(self.websocket.send(msg))
        obs = self.loop.run_until_complete(self.websocket.recv())
        obs = json.loads(obs)
        reward, done, info = 0.0, False, {}
        return obs, reward, done, info

    def close(self):
        self.loop.run_until_complete(self.websocket.close())
