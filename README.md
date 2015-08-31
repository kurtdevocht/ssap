# ssap
Simple Serial Arduino Protocol &amp; Scratch 2 helper

Makes it possible to control Arduino in- and outputs with Scrath 2 (offline)

Tested on an Arduino Leonardo.

Instructions:

- Load the sketch "Arduino Sketches/SSAP/SSAP.ino" on your Arduino
- Now you can connect with the serial port to control the in- and outputs. You could use PuTTY on windows for instance.

Want to control the Arduino with Scratch 2 (offline)?
(Windows only...)

- Start "/Scratch Helper/Bin/ssap.exe"
- This helper application should detect the Arduino automatically
- Start Scratch 2 (offline edition)
- Click the "File" menu while holding the SHIFT button
- Click "Import experimental HTTP extension"
- Select "SSAP_Extension.s2e"
- In the "More blocks" section you'll find the blocks you need to communicate with the Arduino

To initiate polling of the inputs, use the "Use inputs" block, and fill in all inputs you want to use (e.g. "0,3,A1,A6", without the quotes)

