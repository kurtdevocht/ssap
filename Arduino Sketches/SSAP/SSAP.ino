/*
 *    ___  ___  ___  ___ 
 *   / __>/ __>| . || . \
 *   \__ \\__ \|   ||  _/
 *   <___/<___/|_|_||_|  
 *
 *  SSAP = Simple Serial Arduino Protocol
 *
 *  You can download this sketch to your Arduino and start communicating with it over a serial port (Speed = 115200 bps).
 *
 *  All commands must be sent to the Arduino using readable ASCII characters (0x20 .. 0x7E)
 *  and must be ended with a carriage return character (ASCII 13, '\r', 0x0D) OR a newline character (ASCII 10, '\n', 0x0A) OR both
 *
 *  All replies sent from the Arduino are ended with both a carriage return character and a newline character ("\r\n" )
 *
 *  All commands are case insensitive. This means it doesn't matter if you use lower case or upper case letters.
 *
 *  When an invalid command is sent, an error indication ":-(" will be sent back.
 *
 *  SUPPORTED COMMANDS
 *  ------------------
 *  Command:  "WHO ARE YOU?"
 *  Reply:    "SSAP_V01"
 *  Purpose:  This makes it possible to discover an arduino with the SSAP software loaded.
 *            An application could for instance scan all available serial ports,
 *            send "WHO ARE YOU?", and check if "SSAP_V01" is returned.
 *
 * ----------------------
 *
 *  Command:  "{pin}?"          where {pin} is a pin number, like 3, 5, A0, A3,...
 *  Reply:    "{pin}:{value}"   where {pin} is a pin number, like 3, 5, A0, A3,...
 *                              and {value} is a decimal number, like 0, 1, 1023,...
 *
 *  Purpose:  Reads the value of an input pin.
 *            When an analog pin is specified (A1, A3,...), the pinMode will be set to INPUT and an analogRead will be performed (value = 0 .. 1023)
 *            When a digital pin is specified (1, 3,...), the pinMode will be set to INPUT_PULLUP and a digitalRead will be performed (value = 0 or 1)
 * 
 *  Examples:
 * 
 *    Command | Reply
 *    --------|-------
 *    A1?     | A1:567
 *    3?      | 3:1
 *    1?      | 1:0
 *
 * ----------------------
 *
 *  Command:  "{pin}:{value}%"
 *  Reply:    ":-)"
 *
 *  Purpose:  Sets the pinMode to OUPUT and performs an analogWrite.
 *            The value is specified in percent (0% => analogWrite( 0 ); 100% => analogWrite( 255 ) )
 *
 *  Example:  "A0:50%"
 *
 * ----------------------
 *
 *  Command:  "{pin}:{value}A"
 *  Reply:    ":-)"
 *
 *  Purpose:  Sets the pinMode to OUPUT and performs an analogWrite.
 *            The value can range from 0 to 255
 *
 *  Example:  "A0:210"
 *
 * ----------------------
 *
 *  Command:  "{pin}:{angle}*"
 *  Reply:    ":-)"
 *
 *  Purpose:  Attaches a servo motor to the specified pin and sets the motor to the specified angle.
 *            Note: the * symbol is used instead of °, because ° is not a 7-bit ASCII character.
 *
 *  Example:  "9:45*"
 *
 * ----------------------
 *
 *  Command:  "{pin}:{value}"
 *  Reply:    ":-)"
 *
 *  Purpose:  Sets the pinMode to OUPUT and performs an digitalWrite.
 *            If the value is bigger than 0 the pin will be set HIGH. If not, the pin will be set LOW
 *
 *  Example:  "1:1"
 *
 */


#include <Servo.h>

// Commands are received from the serial port byte per byte.
// currentCommand is used to store the part of the commands that has already been received
String currentCommand = "";

// Holds the last millisecond when data from the serial port was received
long lastTimeSerialIn;

// Pins larger than this number will be ignored
#define MAX_PIN 50

// An array of Servo objects, ordered by attached pin
// e.g. 'servos[9]' will contain the Servo object attached to pin 9
Servo servos [ MAX_PIN + 1 ];

// An array that remembers which pinMode has been set for which pin
// e.g. 'pinModes[5]' will contain the pinMode of pin 5
// If no pinMode has been set, the array will contain -1 for that pin
int pinModes [ MAX_PIN + 1 ];

#define CMD_WHO_ARE_YOU "WHO ARE YOU?"
#define REPLY_WHO_ARE_YOU "SSAP_V01"
#define REPLY_ERROR ":-("
#define REPLY_OK ":-)"

// Runs once when the arduino starts
void setup()
{
  Serial.begin( 115200 );
  lastTimeSerialIn = 0;
  
  // We didn't set the pinMode of any pins yet
  // => Initialize the array with pinModes to -1
  for( int i =0; i <= MAX_PIN; i++ )
  {
    pinModes[i] = -1;
  }

  // Wait for serial port to connect. Needed for Leonardo only
  while( !Serial )
  {
  }
}

// Runs all the time over and over again...
void loop()
{
  long now = millis();
  
  // Is there any data available?
  if( Serial.available() <= 0 )
  {
    // There's no new data available on the serial port...
    
    // When a command has been sent 'half'
    // Has it been longer than 5 seconds or longer that new data was received?
    if( now - lastTimeSerialIn > 5000 )
    {
      // => Yes, last data was received more than 5 seonds 
      currentCommand = "";
    }
    
    // No data available on serial port => end the loop
    return;
  }
  
  // Yes, yes! New data is available!
  
  // Remember last time (milliseconds) when whe received the data
  lastTimeSerialIn = now;
  int byteIn = Serial.read();
  
  if( byteIn < 1 )
  {
    // Serial.read() returns -1 if no data is available
    // We should never come here, because Serial.available() returned a number > 0...
    // ...but you can never be safe enough ;-)
    return;
  }
  
  // When one of these characters is received:
  //
  //    Line Feed       = '\n' (ASCII code 10 decimal = 0x0A hexadecimal)
  //    Carriage return = '\r' (ASCII code 13 decimal = 0x0D hexadecimal)
  //
  // the command is complete => Process it
  if( byteIn == '\n' || byteIn == '\r' )
  {
    processCommand( currentCommand ); // Process the command that has been received
    currentCommand = ""; // Forget the command that has been processed
    return;
  }
  
  // Only readable ASCII characters are accepted
  // See http://www.asciitable.com/ for a list of all ASCII characters (" " = 32 = 0x20; "~" = 126 = 0x7E)
  if( byteIn < 32 || byteIn > 126 )
  {
    // Not in a valid range
    // => Ignore this byte...
    return;
  }
  
  // Convert lower case character to upper case
  if( byteIn >= 'a' && byteIn <= 'z' )
  {
    byteIn = byteIn - 'a' + 'A';
  }
  
  // Add the byte (character) to the current command
  currentCommand += char( byteIn );
}

void processCommand( String command )
{
  // Ignore leading and trailing whitespaces
  command.trim();
  
  // Empty command? Do nothing...
  if( command == "" )
  {
    return;
  }
  
  // Request for software version?
  if( command == CMD_WHO_ARE_YOU )
  {
    Serial.println( REPLY_WHO_ARE_YOU );
    return;
  }
  
  // Command format "{pin}?"
  // => Read the value of an input pin
  //
  // => Reply with the analog value for commands like "A1?"
  // => Reply with the digital value for commands like "3?"
  if( command.length() > 1 && command.endsWith( "?" ) )
  {
    String pinString = command.substring( 0, command.length() - 1 ); // 0-based index, but second parameter is 'exclusive'
    if( pinString.startsWith( "A" ) )
    {
      replyAnalogRead( pinString );
    }
    else
    {
      replyDigitalRead( pinString );
    }
    return;
  }
  
  // Command format "{pin}:{value}"
  // => Set the value of an output pin
  if( command.length() > 1 && command.indexOf( ":" ) > -1 )
  {
     String pinString = command.substring( 0, command.indexOf( ":" ) ); // 0-based index, but second parameter is 'exclusive'
     String valueString = command.substring( command.indexOf( ":" ) + 1, command.length()); // 0-based index, but second parameter is 'exclusive'
     setOutput( pinString, valueString );
     return;
  }
  
  // Unknown command? Reply with an error message (sad face)
  Serial.println( REPLY_ERROR );
}

// Reads & return the analog value of a pin
void replyAnalogRead( String pinString )
{
  // Try to make a valid pin number from the String
  int pin = parsePinNumber( pinString );
  if( pin < 0 || pin > MAX_PIN )
  {
    Serial.println( REPLY_ERROR );
    return;
  }
  
  setPinMode( pin, INPUT );
  int value = analogRead( pin );
  Serial.print( pinString + ":" );
  Serial.println( value, DEC );
}

// Reads & returns the digital value of a pin
void replyDigitalRead( String pinString )
{
  // Try to make a valid pin number from the String
  int pin = parsePinNumber( pinString );
  if( pin < 0 || pin > MAX_PIN )
  {
    Serial.println( REPLY_ERROR );
    return;
  }
  
  setPinMode( pin, INPUT_PULLUP );
  int value = digitalRead( pin );
  Serial.print( pinString + ":" );
  Serial.println( value, DEC );
}

// Set the value of a pin
void setOutput( String pinString, String valueString )
{
    int pin = parsePinNumber( pinString );
    if( pin < 0 || pin > MAX_PIN )
    {
      Serial.println( REPLY_ERROR );
      return;
    }  
  
    setPinMode( pin, OUTPUT );
    
    if( valueString.endsWith( "%" ) )
    {
      String percentString = valueString.substring( 0, valueString.length() -1 );
      int value = percentString.toInt();
      
      // 0% => 0; 100% => 255
      value = map( value, 0, 100, 0, 255 );
      
      // Limit value to [0..255]
      value = min( value, 255 );
      value = max( value, 0 );
      analogWrite( pin, value );
    }
    
    else if( valueString.endsWith( "A" ) )
    {
      String analogString = valueString.substring( 0, valueString.length() -1 );
      int value = analogString.toInt();
      
      // Limit value to [0..255]
      value = min( value, 255 );
      value = max( value, 0 );
      analogWrite( pin, value );
    }
    
    else if( valueString.endsWith( "*" ) ) // It would be nicer to use the ° symbol, but that's no 7-bit ASCII character...
    { 
      String angleString = valueString.substring( 0, valueString.length() -1 );
      int value = angleString.toInt();
      servos[pin].attach( pin );
      servos[pin].write( value );
    }
    else
    {
      int value = valueString.toInt();
      if( value > 0 )
      {
        digitalWrite( pin, HIGH );
      }
      else
      {
        digitalWrite( pin, LOW );
      }
    }
    
    Serial.println( REPLY_OK );
}

int parsePinNumber( String pinString )
{
  int pinInt = -1;
  
  if( pinString.startsWith( "A" ) && pinString.length() > 1 )
  {
    String numPart = pinString.substring( 1, pinString.length() );
    
    long number = numPart.toInt();
    switch( number )
    {
      case  0: pinInt = A0; break;
      case  1: pinInt = A1; break;
      case  2: pinInt = A2; break;
      case  3: pinInt = A3; break;
      case  4: pinInt = A4; break;
      case  5: pinInt = A5; break;
      case  6: pinInt = A6; break;
      case  7: pinInt = A7; break;
      case  8: pinInt = A8; break;
      case  9: pinInt = A9; break;
      case 10: pinInt = A10; break;
      case 11: pinInt = A11; break;
    }
  }
  else if( pinString.length() > 0 )
  {
    pinInt = pinString.toInt();
  }
  
  return pinInt;
}

void setPinMode( int pin, int mode )
{
  if( pinModes[ pin ] == mode )
  {
    // Already configured...
    return;
  }
  
  pinMode( pin, mode );
}
