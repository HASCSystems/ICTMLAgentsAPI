# ICT MLAgents API
An 8-connected dense waypoint-based multi-agent reinforcement learning environment capable of handling large realistic environments for training combat-related scenarios using Unity Scenes and Unity MLAgents framework

This is currently using a generic terrain, however the waypoints do not conform to this terrain and it causes some issues.

Rajay Kumar, PhD

[kumar@ict.usc.edu](mailto:kumar@ict.usc.edu)

2023-Jun-28

## Purpose

This document describes the proposed functionality of an API to accommodate a variety of waypoint-based simulations utilizing realistic and non-realistic terrain.

The end-result is to have a clean, modularized, extendible scene that can be easily understood in terms of its functionality and can be performant both in the editor and during runtime.

## Design

The design approach is to build modularly around Unity MLAgents. MLAgents will form the backbone of the code. The goal is to clearly divide the project into sections, including the scene hierarchy. The project will be under source control so it can be checked out and branched as needed.

Modules can be added compositionally. The preference will be to add non-existent modules at runtime if they are not found to already be attached.

An exposed Dictionary can be used to set up modules perhaps.

## Modules - Phase I

## Game Control

A slimmer version of the current ScoutGameController. Controls all other modules.

Implements IMLAgentsGameController

## MLAgents Control

Control foundational aspects of MLAgents:

- Environment Stepping
  - Automatic?
  - Intervals
- Agent step control
  - Ready-for-decision check
    - Followed by explicit decision request
  - Decision Requester
    - Ability to expand decision request time beyond 20 steps

## Waypoints

- Deploy Waypoints
  - Serialize deployment
- Set up connections
- Get neighbors
- Use enum-free design to accommodate different connection schemes (4-connected, 8-connected)
  - Derive cardinal direction from angle
- Rewrite as one class for each waypoint

## Terrain

Ability to bring in complex terrain.

Option to switch to less complex terrain for faster load times.

## Movement

Move with Lerp, navmesh, A\*, or custom

Includes navigation and Dijkstra's algorithm

Implements IWaypointMovement interface

## Agents and Groups

Occlusion checking

Heuristics

Grouping multiple agents (typically heuristic agents)

Teams and groups - part of agent control

Health control (separate component)

## Heuristic Control

Agent heuristics, including state machines.

## Attacks

Hitscan, physics-based. Implemented on a per-agent basis.

Can implement an IAttack interface that can check for an OnHit.

Projectiles will have a separate component.

## Data

Keeping movement data optionally. Also keep track of global variables, such as NeedingAssistance

## Modules - Phase II

## Testing (Including build-ready check)

Make sure agents aren't left on as heuristic so that build doesn't have to be re-done.

Suites to test speed and connections to other waypoints

## GUI

Tracking variables and agent data, like step count

## Visual

Animations, Lighting

## Bug Tracking

Connect to Trello. Automatic reporting.

## Extensions

To include new functionality, it is preferred for classes to be overridden. If any class or function included cannot be overridden or extended, please let the developer know.
