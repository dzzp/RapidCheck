import os
import glob
import h5py
import numpy as np
import cv2
import matplotlib.pyplot as plt
from keras import backend as K

import keras
from keras.models import Sequential
from keras.layers.convolutional import Conv2D, MaxPooling2D
from keras.layers.advanced_activations import LeakyReLU
from keras.layers.core import Flatten, Dense, Activation, Reshape

import tensorflow as tf
import yolo.config as cfg
from utils.help import say

keras.backend.set_image_dim_ordering('th')
weights_path = 'yolo-tiny-epoch17.h5'
is_freeze = True
verbalise = True

def make_yolotiny_network(is_freeze=True):
    model = Sequential()
    model.add(Conv2D(16, (3, 3), input_shape=(3,448,448),padding='same', 
                            activation=LeakyReLU(alpha=0.1), trainable=not is_freeze))
    model.add(MaxPooling2D(pool_size=(2, 2)))
    model.add(Conv2D(32,(3,3), padding='same', 
                            activation=LeakyReLU(alpha=0.1), trainable=not is_freeze))
    model.add(MaxPooling2D(pool_size=(2, 2),padding='valid'))
    model.add(Conv2D(64,(3,3), padding='same', 
                            activation=LeakyReLU(alpha=0.1), trainable=not is_freeze))
    model.add(MaxPooling2D(pool_size=(2, 2),padding='valid'))
    model.add(Conv2D(128,(3,3), padding='same', 
                            activation=LeakyReLU(alpha=0.1), trainable=not is_freeze))
    model.add(MaxPooling2D(pool_size=(2, 2),padding='valid'))
    model.add(Conv2D(256,(3,3), padding='same', 
                            activation=LeakyReLU(alpha=0.1), trainable=not is_freeze))
    model.add(MaxPooling2D(pool_size=(2, 2),padding='valid'))
    model.add(Conv2D(512,(3,3), padding='same', 
                            activation=LeakyReLU(alpha=0.1), trainable=not is_freeze))
    model.add(MaxPooling2D(pool_size=(2, 2),padding='valid'))
    model.add(Conv2D(1024,(3,3), padding='same', activation=LeakyReLU(alpha=0.1), trainable=not is_freeze))
    model.add(Conv2D(1024,(3,3), padding='same', activation=LeakyReLU(alpha=0.1), trainable=not is_freeze))
    model.add(Conv2D(1024,(3,3), padding='same', activation=LeakyReLU(alpha=0.1), trainable=not is_freeze))
    model.add(Flatten())
    model.add(Dense(256))
    model.add(Dense(4096))
    model.add(LeakyReLU(alpha=0.1))
    model.add(Dense(1470))
    return model

model = make_yolotiny_network(is_freeze)
model.summary()

from yolo.training_v1 import darkeras_loss, _TRAINER
from yolo.dataset.data import shuffle

inp_x = model.input
net_out = model.output
sess = K.get_session()

say("Building {} loss function".format(cfg.model_name), verbalise=verbalise)
loss_ph, loss_op = darkeras_loss(net_out)
say("Building {} train optimizer".format(cfg.model_name), verbalise=verbalise)
optimizer = _TRAINER[cfg.trainer](cfg.lr)
gradients = optimizer.compute_gradients(loss_op)
train_op = optimizer.apply_gradients(gradients)

sess.run(tf.global_variables_initializer())
model.load_weights(weights_path)
say("Setting weigths : {}",format(weights_path), verbalise=verbalise)

model.summary()
print(model.output_shape)

batches = shuffle()
for i, (x_batch, datum) in enumerate(batches):
    train_feed_dict = {
       loss_ph[key]:datum[key] for key in loss_ph 
    }
    train_feed_dict[inp_x] = x_batch
    # print("feed_dict.keys() : ", len(train_feed_dict.keys()), train_feed_dict.keys())
    fetches = [train_op, loss_op] 
    fetched = sess.run(fetches, feed_dict=train_feed_dict)
    
    loss_val = fetched[1]
    say("step {} - loss {}".format(i, loss_val), verbalise=True)
    if i == 1:
        save_step_weigths_path = 'yolo-tiny2-step{}.h5'.format(i)
        model.save_weights(save_step_weigths_path)
        print("Saved weigths : ", save_step_weigths_path)
    if i % 3100 == 0:
        model.save_weights('yolo-tiny2-epoch{}.h5'.format(i//3100))
        say("Save weights : ", 'yolo-tiny2-epoch{}.h5'.format(i//3100), verbalise=verbalise)


# In[10]:

sess.close()