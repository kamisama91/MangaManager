#!/usr/bin/env python3
# -*- coding: utf-8 -*-


#@@CALIBRE_COMPAT_CODE_START@@
import sys, os

# Explicitly allow importing everything ...
if os.path.dirname(os.path.dirname(os.path.abspath(__file__))) not in sys.path:
    sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
if os.path.dirname(os.path.abspath(__file__)) not in sys.path:
    sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))

# Bugfix for Calibre < 5:
if "calibre" in sys.modules and sys.version_info[0] == 2:
    from calibre.utils.config import config_dir
    if os.path.join(config_dir, "plugins", "DeDRM.zip") not in sys.path:
        sys.path.insert(0, os.path.join(config_dir, "plugins", "DeDRM.zip"))

# Explicitly set the package identifier so we are allowed to import stuff ...
#__package__ = "DeDRM_plugin"

#@@CALIBRE_COMPAT_CODE_END@@

import sys

__license__ = 'GPL v3'

def uStrCmp (s1, s2, caseless=False):
    import unicodedata as ud
    if sys.version_info[0] == 2:
        str1 = s1 if isinstance(s1, unicode) else unicode(s1)
        str2 = s2 if isinstance(s2, unicode) else unicode(s2)
    else: 
        str1 = s1 if isinstance(s1, str) else str(s1)
        str2 = s2 if isinstance(s2, str) else str(s2)

    if caseless:
        return ud.normalize('NFC', str1.lower()) == ud.normalize('NFC', str2.lower())
    else:
        return ud.normalize('NFC', str1) == ud.normalize('NFC', str2)



# Wrap a stream so that output gets flushed immediately
# and also make sure that any unicode strings get safely
# encoded using "replace" before writing them.
class SafeUnbuffered:
    def __init__(self, stream):
        self.stream = stream
        self.encoding = stream.encoding
        if self.encoding == None:
            self.encoding = "utf-8"
    def write(self, data):
        if isinstance(data,str) or isinstance(data,unicode):
            # str for Python3, unicode for Python2
            data = data.encode(self.encoding,"replace")
        try:
            buffer = getattr(self.stream, 'buffer', self.stream)
            # self.stream.buffer for Python3, self.stream for Python2
            buffer.write(data)
            buffer.flush()
        except:
            # We can do nothing if a write fails
            raise
    def __getattr__(self, attr):
        return getattr(self.stream, attr)
        