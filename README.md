# Command List
### /support \<question\>
Calls the OpenAI API to get a completion request with the prompt as the `question` input. This is specifically designed to be rude and vulgar for the hilarity of it.

`question` - A string containing the prompt for OpenAI.

### /start \<server-name\>
Uses the Azure SDK to turn on a server with the matching `server-name`. Used to turn on game servers like Minecraft. This is useful if you want cheap server hosting, since the server is only turned on when you're using it. ~~This command automatically sets a trigger to turn off the server after 3 hours.~~ This turns off the server after network activity has dropped to near 0 for 20-30ish minutes. It uses a timer function to check on the network status. The VM should always be set to turn off daily in Azure to prevent over spending.

`server-name` - The name of the server to turn on. Provided as a list of options in the Discord interface to prevent mispellings.

### /stop \<server-name\>
Directly turns off the given `server-name`.

`server-name` - The name of the server to turn off. Provided as a list of options in the Discord interface to prevent mispellings.
