# Tachie Preset P3 fixtures

`fixture.json` losslessly zlib/base64-encodes the same `tests/psd_files/1layer.psd` used by canonical Lab #58/#62, from psd-tools/psd-tools commit `5fe6781f32c8d9af39ed9a3786934dd1c26ce212`. The native proof verifies the restored 6,476 bytes and SHA256 before using the file. This avoids a network fetch on each native run. This fixture is not included in the distribution DLL.

The 1px PNGs, Animation preset.ini and PSD preset metadata are generated test-only data. They prove public contract/application behavior, not perceptual quality or compatibility with every third-party PSD plugin.

Source: https://github.com/psd-tools/psd-tools/blob/5fe6781f32c8d9af39ed9a3786934dd1c26ce212/tests/psd_files/1layer.psd

Source license (retained verbatim):

Copyright (c) 2019 Kota Yamaguchi

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in
all copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
THE SOFTWARE.
