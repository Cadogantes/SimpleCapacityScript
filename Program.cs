//------------------------------------ Instructions ------------------------------------//
//================================================//
//This script will allow you to display current cargo capacity in your ship's cockpit. To use it follow this steps:
//1) Install Programmable Block on your ship
//2) Change the "cockpitName" and "cockpitScreen" variables in the config section of the script, a bit below
//3) Copy the whole script after changes (if you didn't get the script in game)
//4) While on the ship - go to Control Panel, select Programmable Block, click "Edit" button, paste the script there, click "OK", click "Run"
//-------------------------------- End of Instructions --------------------------------//
//================================================//

//------------------------------------ Config ------------------------------------//
//================================================//
readonly string cockpitName = "Cockpit"; //name of your Cockpit block
readonly int cockpitScreen = 0; //indexed from 0, changes the screen on which capacity info will be displayed. Try values from 0 to 4 to identify which scren is the one you want.
readonly int barLength = 50; //how long the indicator bar is
readonly int topPositions = 3; //how many top items in cargo would you like to see displayed

readonly bool debug = false;
//-------------------------------- End of Config --------------------------------//
//================================================//

//global variables
public float currentCapacity;   //current cargo
public float maxCapacity;       //max cargo
public float capacityInPercent; //ratio of current to max cargo
List<IMyFunctionalBlock> tools;
List<IMyCargoContainer> cargoContainers;
List<IMyShipConnector> shipConnectors;

public Program()
{
    Runtime.UpdateFrequency = UpdateFrequency.Update100; //The number at the end represents how often the script updates. 1 is the fastest, 10 is "medium", 100 is the slowest. Slow updates means lesser impact on game performance
}

public void Main()
{
    try
    {
        ValidateConfig(); //checks if configuration is valid
    }
    catch
    {
        return; //if configuration validation fails - stop the script
    }
    DoDiagnostics(); //detects and counts ship's elements
    CalculateCapacity(); //calculate max capacity and current capacity

    string capacityInfo = BuildInfoStringCapacity();    //return string with info about current, max and free cargo
    string topItems = BuildInfoStringTopMass(topPositions); //builds a string with top 3 types of items in cargo by amount 

    //merge texts that are to be displayed
    string textOutput = capacityInfo + "\n\n"
                                + topItems;

    //state will represent low - medium - high cargo. I use it in DisplayInCockpit method to add color indicator of current load
    int state = 0;
    if (capacityInPercent > 30) state = 1;
    if (capacityInPercent > 80) state = 2;
    DisplayInCockpit(textOutput, state);

}

//creates progress bar string
public string CreateProgressBar(int fillPercentage)
{
    //Progress bar display variables
    string barFull = "|";
    string barEmpty = "'";
    string barStart = "{";
    string barEnd = "}";

    string createdBar = barStart;  //start building result string

    int filledBars = (int)(barLength * ((float)fillPercentage / 100));
    DebugEcho($"fillPercentage: {fillPercentage}, filledBars: {filledBars}");
    int emtyBars = barLength - filledBars;

    //start by adding full bars to the string - add filledBars of them
    for (int i = filledBars; i > 0; i--)
    {
        createdBar += barFull;
    }

    //then add empty bars to the string
    for (int i = emtyBars; i > 0; i--)
    {
        createdBar += barEmpty;
    }

    //finally close the string
    createdBar += barEnd;

    return createdBar;
}

//detects and aggregates ship's tool blocks
public void DetectShipTools()
{
    List<IMyShipDrill> drills = new List<IMyShipDrill>();
    List<IMyShipWelder> welders = new List<IMyShipWelder>();
    List<IMyShipGrinder> grinders = new List<IMyShipGrinder>();
    tools = new List<IMyFunctionalBlock>();
    GridTerminalSystem.GetBlocksOfType<IMyShipDrill>(drills, block => block.IsSameConstructAs(Me));
    if (drills != null && drills.Count() > 0)
    {
        foreach (var drill in drills)
        {
            tools.Add((IMyFunctionalBlock)drill);
        }
    }
    GridTerminalSystem.GetBlocksOfType<IMyShipWelder>(welders, block => block.IsSameConstructAs(Me));
    if (welders != null && welders.Count() > 0)
    {
        foreach (var welder in welders)
        {
            tools.Add((IMyFunctionalBlock)welder);
        }
    }
    GridTerminalSystem.GetBlocksOfType<IMyShipGrinder>(grinders, block => block.IsSameConstructAs(Me));
    if (grinders != null && grinders.Count() > 0)
    {
        for (int i = 0; i < grinders.Count(); i++)
        {
            tools.Add((IMyFunctionalBlock)grinders[i]);
        }
    }
}

//detects and aggregates cargo containers, connectors and tools. I.e. blocks that can have items inside
private void DoDiagnostics(bool echo = true)
{
    cargoContainers = new List<IMyCargoContainer>();
    GridTerminalSystem.GetBlocksOfType(cargoContainers, block => block.IsSameConstructAs(Me));

    shipConnectors = new List<IMyShipConnector>();
    GridTerminalSystem.GetBlocksOfType(shipConnectors, block => block.IsSameConstructAs(Me));

    DetectShipTools();

    if (echo)
    {
        Echo($"Tools: {tools.Count()}");
        Echo($"Cargo Containers: {cargoContainers.Count()}");
        Echo("");
    }
}

//checks if the config data are correct
private void ValidateConfig()
{
    //validate if cockpit block exists
    var cockpitBlock = GridTerminalSystem.GetBlockWithName(cockpitName);
    if (cockpitBlock == null)
    {
        Echo($"{cockpitName} not found on the ship. Change the 'cockpitName' variable in configuration section of the script");
        throw new Exception("Cockpit name validation failed");
    }

    //validate if screen exists
    var screen = cockpitBlock is IMyTextSurface
                    ? (IMyTextSurface)cockpitBlock
                    : ((IMyTextSurfaceProvider)cockpitBlock).GetSurface(cockpitScreen);
    if (screen == null)
    {
        Echo($"Cockpit screen with index {cockpitScreen} not found. Change the 'cockpitScreen' index in configuration section of the script");
        throw new Exception("Cockpit name validation failed");
    }


    Echo("Configuration validated");
}

//calculates current and max cargo capacity
private void CalculateCapacity()
{

    maxCapacity = 0;
    currentCapacity = 0;

    for (int i = 0; i < cargoContainers.Count; i++)
    {
        var inventory = cargoContainers[i].GetInventory(0);
        maxCapacity += (float)inventory.MaxVolume;
        currentCapacity += (float)inventory.CurrentVolume;
    }

    for (int i = 0; i < shipConnectors.Count; i++)
    {
        var inventory = shipConnectors[i].GetInventory(0);
        maxCapacity += (float)inventory.MaxVolume;
        currentCapacity += (float)inventory.CurrentVolume;
    }

    for (int i = 0; i < tools.Count; i++)
    {
        var inventory = tools[i].GetInventory(0);
        maxCapacity += (float)inventory.MaxVolume;
        currentCapacity += (float)inventory.CurrentVolume;
    }
}

//displays 'text' in cockpit and colors it according to 'state'
private void DisplayInCockpit(string text, int state)
{
    var block = GridTerminalSystem.GetBlockWithName(cockpitName);
    var surface = block is IMyTextSurface
                        ? (IMyTextSurface)block
                        : ((IMyTextSurfaceProvider)block).GetSurface(cockpitScreen);
    surface.ContentType = ContentType.TEXT_AND_IMAGE;
    surface.WriteText(text);

    //color coding cargo state
    if (state == 0) //low cargo
    {
        surface.FontColor = Color.Green;
    }
    else if (state == 1) //medium cargo
    {
        surface.FontColor = Color.Yellow;
    }
    else if (state == 2) //high cargo
    {
        surface.FontColor = Color.Red;
    }
}

public string BuildInfoStringTopMass(int positions) //builds a string with top {positions} of heaviest items in cargo
{
    //make a list of items in ship's inventory
    List<MyInventoryItem> itemsInCargo = new List<MyInventoryItem>();

    //get all blocks with cargo
    List<IMyTerminalBlock> allBlocksWithCargo = new List<IMyTerminalBlock>();
    foreach (var container in cargoContainers)
    {
        allBlocksWithCargo.Add(container);
    }
    foreach (var connector in shipConnectors)
    {
        allBlocksWithCargo.Add(connector);
    }
    foreach (var tool in tools)
    {
        allBlocksWithCargo.Add(tool);
    }

    //get items in cargo
    itemsInCargo.AddRange(GetItemsInBlocks(allBlocksWithCargo));
    DebugEcho("All inventory items count: " + allBlocksWithCargo.Count);

    //merge elements of inventory list by type to get single stacks of every item
    List<SimplifiedInventoryItem> simplifiedItemsInCargo = ConvertToSimplifiedInventory(itemsInCargo);
    DebugEcho("Simplified items count: " + simplifiedItemsInCargo.Count);

    simplifiedItemsInCargo = MergeItemsOfSameType(simplifiedItemsInCargo);
    DebugEcho("Merged items count: " + simplifiedItemsInCargo.Count);

    //sort those items by their amount
    var sortedItemsInCargo = simplifiedItemsInCargo.OrderByDescending(x => x.Amount).ToList(); // I wanted to compare volumes to be in line with measuring cargo in volume, but it turns out to be problematic. Mass is the simplest workaround as it should be good enough to compare inventory

    //build display string
    int displayedItems = sortedItemsInCargo.Count < positions ? sortedItemsInCargo.Count : positions;
    string result = $"Top {displayedItems} " + (string)(displayedItems == 1 ? "item" : "items") + " in cargo:";
    for (int i = 0; i < displayedItems; i++)
    {
        result += "\n" + sortedItemsInCargo[i].Type.SubtypeId + " – " + String.Format("{0:#,##0}", sortedItemsInCargo[i].Amount);
    }
    Echo(result);

    return result;
}

//returns list of items in blocks
public List<MyInventoryItem> GetItemsInBlocks(List<IMyTerminalBlock> blocksWithInventory)
{
    List<MyInventoryItem> itemsInBlocks = new List<MyInventoryItem>();
    foreach (var block in blocksWithInventory)
    {
        IMyInventory blockInventory = block.GetInventory(0); //get the inventory of block. Some have more than one, but for simplicity sake this should be good enough 0
        List<MyInventoryItem> itemsInBlock = new List<MyInventoryItem>(); //declare a list of items in block
        blockInventory.GetItems(itemsInBlock); //fill the list
        itemsInBlocks.AddRange(itemsInBlock);    //add list of item in this block to list of items in all input blocks
    }

    return itemsInBlocks;
}

//merges list so that only one element of each type exists within
public List<SimplifiedInventoryItem> MergeItemsOfSameType(List<SimplifiedInventoryItem> listToMerge)
{

    // Group by Subtype and sum Amount
    List<SimplifiedInventoryItem> mergedList = listToMerge
            .GroupBy(item => item.Type.SubtypeId)
            .Select(group => new SimplifiedInventoryItem
            (
                amount:group.Sum(item => item.Amount),
                type:group.First().Type // Use the Type from the first item in the group
            ))
            .ToList();

    return mergedList;
}

//just for conveniance sake I add this method that converts MyInventoryItem list to a list of SimplifiedInventoryItem
public List<SimplifiedInventoryItem> ConvertToSimplifiedInventory(List<MyInventoryItem> listToConvert)
{
    List<SimplifiedInventoryItem> convertedList = new List<SimplifiedInventoryItem>();

    foreach (var listElement in listToConvert)
    {
        convertedList.Add(new SimplifiedInventoryItem(listElement.Type, (long)listElement.Amount));
    }

    return convertedList;
}

//this class proved to be required to merge all items in inventory list, as Amount property of MyInventoryItem is read only
public class SimplifiedInventoryItem
{
    public long Amount { get; set; }
    public MyItemType Type { get; set; }

    public SimplifiedInventoryItem(MyItemType type, long amount)
    {
        Amount = amount;
        Type = type;
    }
    public override string ToString()
    {
        return $"{String.Format("{0:#,##0}", this.Amount)}" + " " + this.Type.SubtypeId.ToString();
    }
}

public string BuildInfoStringCapacity()
{
    string capacityInfo = String.Format($"Cargo: {(int)currentCapacity}m³ / {(int)maxCapacity}m³");

    capacityInPercent = currentCapacity / maxCapacity * 100; //calculate current/max ratio to build progress bar
    string capacityBar = CreateProgressBar((int)capacityInPercent);

    //some additional info
    string freeCapacity = (int)(maxCapacity - currentCapacity) + " m³ free";

    return capacityInfo + "\n"
        + capacityBar + "\n"
        + freeCapacity;
}

public void DebugEcho(string message)
{
    if (debug) Echo(message);
}

