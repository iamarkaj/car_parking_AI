#!/usr/bin/env python

import torch
torch.manual_seed(1337)
import torch.nn as nn
import torch.optim as optim
import torch.autograd as autograd
import torch.nn.functional as F
import numpy as np
np.random.seed(1337)
from noisy_linear import NoisyLinear



class Agent(nn.Module):

    def __init__(self, input_shape, num_atoms, num_actions = 4):
        super(Agent, self).__init__()

        self.input_shape = input_shape
        self.num_actions = num_actions
        self.num_atoms   = num_atoms
        self.features    = nn.Sequential(
            nn.Conv2d(input_shape[0], 32, kernel_size = 8, stride = 4),
            nn.ReLU(),
            nn.Conv2d(32, 64, kernel_size = 4, stride = 2),
            nn.ReLU(),
            nn.Conv2d(64, 64, kernel_size = 3, stride = 1),
            nn.ReLU())
        self.noisy_value1     = NoisyLinear(self.features_size(), 512)
        self.noisy_value2     = NoisyLinear(512, self.num_atoms)
        self.noisy_advantage1 = NoisyLinear(self.features_size(), 512)
        self.noisy_advantage2 = NoisyLinear(512, self.num_atoms * self.num_actions)

    def features_size(self):
        return self.features(torch.zeros(1, *self.input_shape)).view(1, -1).size(1)

    def forward(self, x):
        batch_size = x.size(0)
        x          = self.features(x)
        x          = x.view(batch_size, -1)
        value      = F.relu(self.noisy_value1(x))
        value      = self.noisy_value2(value)
        advantage  = F.relu(self.noisy_advantage1(x))
        advantage  = self.noisy_advantage2(advantage)
        value      = value.view(batch_size, 1, self.num_atoms)
        advantage  = advantage.view(batch_size, self.num_actions, self.num_atoms)
        x          = value + advantage - advantage.mean(1, keepdim=True)
        x          = x.view(-1, self.num_actions, self.num_atoms)
        return x

    def reset_noise(self):
        self.noisy_value1.reset_noise()
        self.noisy_value2.reset_noise()
        self.noisy_advantage1.reset_noise()
        self.noisy_advantage2.reset_noise()

    def act(self, state, epsilon):
        if np.random.rand() > epsilon:
            with torch.no_grad():
                state   = torch.FloatTensor(state).unsqueeze(0)
            qvalues = self.forward(state).mean(2)
            action  = qvalues.max(1)[1]
            action  = action.data.cpu().numpy()[0]
        else:
            action  = np.random.randint(self.num_actions)
        return action
