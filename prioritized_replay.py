#!/usr/bin/env python


import torch
torch.manual_seed(1337)
import torch.nn as nn
import torch.optim as optim
import torch.autograd as autograd
import torch.nn.functional as F
import numpy as np
np.random.seed(1337)


class PrioritizedReplayBuffer(object):

    def __init__(self, capacity, load, alpha=0.6):
        self.capacity   = capacity
        self.alpha      = alpha
        if load:
            self.buffer     = np.array(np.load("store/buffer.npy", allow_pickle=True), dtype= np.float32)
            self.priorities = np.array(np.load("store/priorities.npy", allow_pickle=True), dtype=np.float32)
            self.position   = int(np.load("store/position.npy", allow_pickle=True))
        else:
            self.buffer     = np.array([(0,0,0,0)], dtype= np.float32)
            self.priorities = np.zeros((self.capacity), dtype= np.float32)
            self.position   = 0

    def push(self, current_state, action, new_state, reward):
        self.priorities[self.position] = self.priorities.max() if len(self.buffer) else 1.0
        if len(self.buffer) < self.capacity:
            self.buffer = np.vstack((self.buffer, (current_state, action, new_state, reward)))
        else:
            self.buffer[self.position] = (current_state, action, new_state, reward)
        self.position = (self.position + 1) % self.capacity
        return

    def sample(self, size, beta=0.4):
        prios       = self.priorities[:] if len(self.buffer) == self.capacity else self.priorities[:self.position]
        prios       = prios**self.alpha
        prios       = np.nan_to_num(prios)
        prios_sum   = np.sum(prios)
        prios       = prios / prios_sum
        prb         = None if np.isnan(prios).any() else prios
        indices     = np.random.choice(len(self.buffer), size, p=prb)
        states      = [self.buffer[id][0] for id in indices]
        actions     = [self.buffer[id][1] for id in indices]
        next_states = [self.buffer[id][2] for id in indices]
        rewards     = [self.buffer[id][3] for id in indices]
        total       = len(self.buffer)
        weights     = np.array([(total * prios[id]) ** (-beta) for id in indices], dtype=np.float32)
        weights_max = weights.max()
        weights     = weights / weights_max
        return states, actions, next_states, rewards, indices, weights

    def update_priorities(self, batch_idx, batch_prios):
        for b_idx, b_prios in zip(batch_idx, batch_prios):
            self.priorities[b_idx] = b_prios

    def __len__(self):
        return len(self.buffer)
