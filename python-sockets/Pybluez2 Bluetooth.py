# -*- coding: utf-8 -*-
"""Bluetooth.ipynb

Automatically generated by Colab.

Original file is located at
    https://colab.research.google.com/drive/1e27pInmecjMrpz2zFh_Hvg8gh2vb1rF5
"""

#install pybluez2
import bluetooth
nearby_devices = bluetooth.discover_devices(lookup_names=True)
print("found %d devices" % len(nearby_devices))

for addr, name in nearby_devices:
     print(" %s - %s" % (addr, name))