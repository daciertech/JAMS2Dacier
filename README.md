# JAMS2Dacier

This repository will help you convert JAMS objects to Dacier Scheduler objects.
This utility is packages as a dotnet tool so you can simply install it and run it.
Or, if you want to customize the conversion, you can clone the repository and build it yourself.

## Before You Start
You can use this tool without a Dacier Scheduler instance but you won't be able to apply the converted YAML files to a Dacier Scheduler instance until you have one set up.
You might want to use this tool before you create a Dacier Scheduler instance to gauge how much work it will be to convert your JAMS objects to Dacier Scheduler objects.
Our converter does not support all JAMS objects or execution methods.

You can find instructions on how to set up a Dacier Scheduler instance in the Dacier Scheduler Getting Started repository at https://github.com/daciertech/SchedulerStartup.

## Installation

You can install the tool globally using the following command:
```bash
dotnet tool install -g Dacier.JAMS2Dacier
```

You will also need to have the Dacier Scheduler CLI tool installed. If you have not already installed this too you can install it globally using the following command:
```bash
dotnet tool install -g Dacier.SchedulerCLI
```

## Usage

Conversion is a three step process, first you extract the JAMS objects to XML files, then you convert those XML files to Dacier Scheduler YAML files then apply that YAML to your Dacier Scheduler instance..

### Step 1: Extract JAMS objects to XML files
```bash
jams2dacier extract localhost C:\Your\Output\Directory\
```

'localhost' is the name of your JAMS instance, and 'C:\Your\Output\Directory\' is the directory where you want to save the XML files.

### Step 2: Convert XML files to Dacier Scheduler YAML files
```bash
jams2dacier convert C:\Your\Output\Directory\ C:\Your\Output\Directory\Dacier\
```

### Step 3: Apply YAML files to Dacier Scheduler instance
```bash
schedulercli apply -f C:\Your\Output\Directory\Dacier\
```

## Customization

There are two levels of customization, first you can edit the 'method-mapping.json' file to change how JAMS execution methods are mapped to Dacier Scheduler jobs.
You will need to do this if you have created your own JAMS execution methods.

The second level of customization is to clone the repository and modify the code to fit your specific needs. The code is well documented and should be easy to understand.

