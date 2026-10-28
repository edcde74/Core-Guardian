from quant import quantize
from qc import finalize
import numpy as np
def final(L):
    a = finalize(L.a)
    keep = L.keep & (L.a[:, :, 3] > 0)
    return finalize(quantize(a, keep=keep))
