#!/usr/bin/env python


"""Implementing Rainbow-IQN
   a Reinforcement Learning algorithm
"""

load = False # set load = True to resume training


import torch
torch.manual_seed(1337)
import torch.nn as nn
import torch.optim as optim
import torch.autograd as autograd
import torch.nn.functional as F
import numpy as np
np.random.seed(1337)
import gc
import io
import cv2
import imageio
from PIL import Image
from PIL import ImageFile
import matplotlib.pyplot as plt
get_ipython().run_line_magic('matplotlib', 'inline')
ImageFile.LOAD_TRUNCATED_IMAGES = True
from IPython.display import clear_output
import socket
import time
from math import sqrt

from noisy_linear import NoisyLinear
from prioritized_replay import PrioritizedReplayBuffer
from agent import Agent


def update_target(current_model, target_model):
    target_model.load_state_dict(current_model.state_dict())


def compute_td_loss(batch_size, beta):
    state, action, next_state, reward, indices, weights = replay_buffer.sample(batch_size, beta)
    state      = torch.FloatTensor(np.float32(state))
    action     = torch.LongTensor(action)
    next_state = torch.FloatTensor(np.float32(next_state))
    reward     = torch.FloatTensor(reward)
    weights    = torch.FloatTensor(weights)
    theta      = current_model(state)[np.arange(batch_size), action]
    Znext      = target_model(next_state).detach()
    Znext_max  = Znext[np.arange(batch_size), Znext.mean(2).max(1)[1]]
    Ttheta     = reward.unsqueeze(1) + gamma * Znext_max
    diff       = Ttheta.t().unsqueeze(-1) - theta
    huber_diff = torch.where(diff.abs() < 1, 0.5 * diff.pow(2), 1 * (diff.abs() - 0.5 * 1))
    loss       = huber_diff * (tau - (diff.detach() < 0).float()).abs()
    loss       = loss.mean()
    prios      = loss * weights + 1e-3
    optimizer.zero_grad()
    loss.backward()
    replay_buffer.update_priorities(indices, prios.data.cpu().numpy())
    nn.utils.clip_grad_norm_(current_model.parameters(), 0.5)
    optimizer.step()
    current_model.reset_noise()
    target_model.reset_noise()
    return loss


def plot(frame_idx, rewards, losses):
    clear_output(True)
    plt.figure(figsize=(20,5))
    plt.subplot(131)
    plt.title(f'Frame: {frame_idx}')
    plt.plot(rewards)
    plt.subplot(132)
    plt.title('Loss')
    plt.plot(losses)
    plt.show()


def preprocess_frame(binary_data):
    x = None
    x = Image.open(io.BytesIO(binary_data))
    x = np.array(x)
    x = cv2.cvtColor(x, cv2.COLOR_BGR2GRAY)
    x = cv2.resize(x, (input_shape[1], input_shape[2]))
    x = x / 255.0
    return x


def save_model():
    torch.save(current_model, "E:/Jupyter files/REINFORCEMENT LEARNIING/CarParkingAI/store/current_model")
    torch.save(target_model, "E:/Jupyter files/REINFORCEMENT LEARNIING/CarParkingAI/store/target_model")


def load_model():
    current_model = torch.load("E:/Jupyter files/REINFORCEMENT LEARNIING/CarParkingAI/store/current_model")
    target_model  = torch.load("E:/Jupyter files/REINFORCEMENT LEARNIING/CarParkingAI/store/target_model")


beta_start     = 0.4
beta_frames    = 10_000
update_beta    = lambda frame_idx: min(1.0, beta_start + frame_idx * (1.0 - beta_start) / beta_frames)

epsilon_decay  = 0.002
min_epsilon    = 0.1
update_epsilon = lambda frame_idx: min_epsilon + (1.0 - min_epsilon) * np.exp(-epsilon_decay * frame_idx)


input_shape         = (4, 84, 84)
num_actions         = 4
BATCH_SIZE          = 256
MINI_BATCH_SIZE     = 32
gamma               = 0.99
num_atoms           = 51
computes_loss_after = 4
plot_after          = 10
copy_weights_after  = 100
MIN_REPLAY_SIZE     = 20_000
tau                 = torch.Tensor((2 * np.arange(num_atoms) + 1) / (2.0 * num_atoms)).view(1, -1)


current_model = Agent(input_shape, num_atoms, num_actions)
target_model  = Agent(input_shape, num_atoms, num_actions)
update_target(current_model, target_model)

optimizer     = optim.Adam(current_model.parameters(), lr=0.0001)

replay_buffer = PrioritizedReplayBuffer(200_000, load)


if load:
    load_model()


def main(load):

    counter     = int(np.load("store/counter.npy", allow_pickle=True)) if load else 0
    epsilon     = 1.0
    all_rewards = np.array([])
    losses      = np.array([])

    print("[SERVER] Starting ...")
    s = socket.socket()
    s.bind(('127.0.0.1', 8010))
    s.listen()
    while True:
        c, addr = s.accept()
        try:
            while True:
                counter       = counter + 1
                total_rewards = 0

                c.sendall("1".encode('ascii')) # send 1 to start receiving data
                for batch_num in range(BATCH_SIZE):
                    old_frame_state = np.zeros(input_shape)
                    new_frame_state = np.zeros(input_shape)
                    for frame_num in range(input_shape[0]):
                        old_frame_len = c.recv(15).decode('utf-8')  #receive old image len first
                        old_frame     = c.recv(int(old_frame_len))  #receive old image
                        old_frame_state[frame_num] = preprocess_frame(old_frame)
                    action = current_model.act(old_frame_state, epsilon)
                    epsilon = update_epsilon(counter)
                    c.sendall(str(action).encode('ascii'))  #send action
                    for frame_num in range(input_shape[0]):
                        new_frame_len = c.recv(15).decode('utf-8')  #receive image len first
                        new_frame     = c.recv(int(new_frame_len))  #receive image
                        new_frame_state[frame_num] = preprocess_frame(new_frame)
                    reward = float(c.recv(10).decode('utf-8')) / 20  #receive reward
                    replay_buffer.push(old_frame_state, action, new_frame_state, reward)
                    total_rewards += reward
                    del old_frame, new_frame, old_frame_state, new_frame_state
                    gc.collect()
                c.sendall("0".encode('ascii')) # send 0 to stop receiving data


                all_rewards = np.append(all_rewards, total_rewards)


                if len(replay_buffer) > MIN_REPLAY_SIZE:
                    if counter % computes_loss_after == 0:
                        beta   = update_beta(counter)
                        loss   = compute_td_loss(MINI_BATCH_SIZE, beta)
                        losses = np.append(losses, loss.item())
                    if counter % copy_weights_after == 0:
                        update_target(current_model, target_model)
                    if counter % plot_after == 0:
                        #plot(counter, all_rewards, losses)
                else:
                    print(counter)

        except Exception as e:
            print(e)


        finally:
            np.save("store/counter.npy", counter)
            np.save("store/position.npy", replay_buffer.position)
            np.save("store/buffer.npy", replay_buffer.buffer)
            np.save("store/priorities.npy", replay_buffer.priorities)
            save_model()
            gc.collect()
            c.close()
            return

    print("[SERVER] Stoping ...")


if __name__ == "__main__":
    main(load)
