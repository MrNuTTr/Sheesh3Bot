# Command List
### /support \<question\>
Calls the OpenAI API to get a completion request with the prompt as the `question` input. This is specifically designed to be rude and vulgar for the hilarity of it.

`question` - A string containing the prompt for OpenAI.

### /turn-on \<server-name\>
Uses the Azure SDK to turn on a server with the matching `server-name`. Used to turn on game servers like Minecraft. This is useful if you want cheap server hosting, since the server is only turned on when you're using it. This command automatically sets a trigger to turn off the server after 3 hours.

`server-name` - The name of the server to turn on. Provided as a list of options in the Discord interface to prevent mispellings.
