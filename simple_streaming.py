#!/usr/bin/env python

"""
simple video streaming from Unity simulation to perform cv2 operation in python
"""


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
import numpy as np


s = socket.socket()
s.bind(('127.0.0.1', 8010)) #port is 8010
s.listen()


print("[SERVER] Starting ...")


while True:


    c, addr = s.accept() #accept connection

    try:


        while True:


            img = c.recv(1000000)
            img = Image.open(io.BytesIO(img))
            img = np.array(img)


            cv2_transform_img = cv2.cvtColor(img, cv2.COLOR_BGR2GRAY)
            cv2_transform_img = cv2.GaussianBlur(cv2_transform_img, (5,5), 1)
            cv2_transform_img = cv2.adaptiveThreshold(cv2_transform_img, 255,
                                                      cv2.ADAPTIVE_THRESH_GAUSSIAN_C, cv2.THRESH_BINARY, 11, 3

                                                     )
            cv2_transform_img = cv2.resize(cv2_transform_img, (128,128))


            plt.figure(figsize = (8,8))
            plt.imshow(cv2_transform_img, cmap='gray')
            plt.show()


            clear_output(wait=True)


    except Exception as e:
        print(e)


    finally:
        c.close() #close connection


print("[SERVER] Closed.")

