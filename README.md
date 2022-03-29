This repository provides a toolkit of source code projects that target the .NET Framework v4, and also (in LFS) the graphics assets needed by the software, along with the documents that should be printed out and distributed to the participants.

The Visual Studio solution file AllProjects.sln includes every project provided in the solution.  A first step is to confirm that this solution compiles correctly.

Four types of simulation are provided by the toolkit: IT Service Management (ITSM), Project Management (PM), Cloud and DevOps.  For each simulation type available, there is an associated <simulation_type>.Application project, which should be set as the startup project.

Certain code subsystems have been removed, including:
1. Any modules which modelled the behaviour of product offerings of particular clients
2. Any modules relating to licensing, encryption or other security-related matters
3. Any modules incorporating code subject to a different license from the one applied to this release

Similarly, trademarks, copyright notices and company names have been redacted from all the graphics files.

Accordingly, to build working simulation software from this Toolkit, it will be necessary to replace the functionality of these redacted systems.  The first starting point should be the project called Licensor.  This provides interfaces and stub implementations for the functionality that you will want to implement in your own conditional access system.

At this point you will have a running simulation.  However, it will error out as soon as you run a game, because there will be missing "audio" and "video" folders.  You will need to provide these, and supply suitable files to satisfy the file loads that generate errors.

You will also of course need to re-brand the graphics in the "images" and "kit" folders, but this is a cosmetic rather than functional matter.

Beyond this point, you will need to supply code to implement the generating of PDF reports etc.
