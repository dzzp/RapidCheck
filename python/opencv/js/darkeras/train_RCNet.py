import h5py
import numpy as np
import cv2
import matplotlib.pyplot as plt
import tensorflow as tf
import yolo.config as cfg
from utils.help import say, conv_weigths_flatten

import keras.backend as K
from yolo.net.yolo_tiny_thdim_net import yolo_tiny_THdim_model, yolo_shortdense_THdim_model

if cfg.image_dim_order == 'th':
	K.set_image_dim_ordering('th')

# pretrain_weight_path = 'models/pretrain/yolo-tiny-origin-thdim-named.h5'
pretrain_weight_path = 'models/train/yolo-2class-voc2007-base-shortdense-cell14-steps24000.h5'
# pretrain_weight_path = 'models/train/yolo-2class-complete.h5'
# pretrain_weight_path = 'models/train/yolo-2class-mydata-3video-complete.h5'
is_freeze = True
verbalise = True

freeze_layer_weights = None
trainable_layer_weights = None
show_trainable_state = False # 여기를 True 로 바꾸면, conv layer 와 dense layer 의 학습별 weigths 가 변하는지 안변하는지를 확인할 수 있다.
trained_save_weights_prefix = 'models/train/{}-'.format(cfg.model_name)

print(cfg.dataset_abs_location)

sess = tf.Session()
K.set_session(sess)

# model = yolo_tiny_THdim_model()
model = yolo_shortdense_THdim_model()
model.summary()

from yolo.training_v1 import darkeras_loss, _TRAINER
from yolo.datacenter.data import shuffle

inp_x = model.input
net_out = model.output

say("Building {} loss function".format(cfg.model_name), verbalise=verbalise)
loss_ph, loss_op = darkeras_loss(net_out)
say("Building {} train optimizer".format(cfg.model_name), verbalise=verbalise)
optimizer = _TRAINER[cfg.trainer](cfg.lr)
gradients = optimizer.compute_gradients(loss_op, var_list=model.trainable_weights)
train_op = optimizer.apply_gradients(gradients)

sess.run(tf.global_variables_initializer())
	
model.load_weights(pretrain_weight_path, by_name=True)

batches = shuffle()
for i, (x_batch, datum) in enumerate(batches, start=24000):
	train_feed_dict = {
	   loss_ph[key]:datum[key] for key in loss_ph 
	}
	train_feed_dict[inp_x] = x_batch
	# print("feed_dict.keys() : ", len(train_feed_dict.keys()), train_feed_dict.keys())
	fetches = [train_op, loss_op] 
	fetched = sess.run(fetches, feed_dict=train_feed_dict)
	
	loss_val = fetched[1]
	say("step {} - loss {}".format(i, loss_val), verbalise=True)
	
	if show_trainable_state:
		conv1 = model.layers[0]
		dense_last = model.layers[-1]
		if freeze_layer_weights is None:
			freeze_layer_weights = sess.run(conv1.weights[0])
			trainable_layer_weights = sess.run(dense_last.weights[0])
		else:
			freeze_layer_comp = (freeze_layer_weights == sess.run(conv1.weights[0]))
			if all(conv_weigths_flatten(freeze_layer_comp)):
				print(conv1.name, "\tFreeze!")
			else:
				print(conv1.name, "\tTraining~")

			last_layer_comp = (trainable_layer_weights == sess.run(dense_last.weights[0]))
			last_layer_flatten = [val for sublist in last_layer_comp for val in sublist]
			if all(last_layer_flatten):
				print(dense_last.name, "\tFreeze!")
			else:
				print(dense_last.name, "\tTraining~")
	
	# if i == 0:
	# 	model.save_weights(trained_save_weights_prefix + 'steps{}.h5'.format(i))
	# 	say("Save weights : ", trained_save_weights_prefix + 'steps{}.h5'.format(i), verbalise=verbalise)
   
	if i % 4000 == 0:
		model.save_weights(trained_save_weights_prefix + 'steps{}.h5'.format(i))
		say("Save weights : ", trained_save_weights_prefix + 'steps{}.h5'.format(i), verbalise=verbalise)

say('Training All Done..', verbalise=verbalise)
model.save_weights(trained_save_weights_prefix + 'complete.h5')
say("Save weights : ", trained_save_weights_prefix + 'complete.h5', verbalise=verbalise)