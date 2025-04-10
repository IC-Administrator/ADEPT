# Software Requirements Specification (SRS)

## For Adept AI Teaching Assistant

**Version 1.0**  
**Date: April 8, 2025**

## Table of Contents

1. [Introduction](#1-introduction)
   1. [Purpose](#11-purpose)
   2. [Scope](#12-scope)
   3. [Definitions, Acronyms, and Abbreviations](#13-definitions-acronyms-and-abbreviations)
   4. [References](#14-references)
   5. [Overview](#15-overview)
2. [Overall Description](#2-overall-description)
   1. [Product Perspective](#21-product-perspective)
   2. [Product Functions](#22-product-functions)
   3. [User Characteristics](#23-user-characteristics)
   4. [Constraints](#24-constraints)
   5. [Assumptions and Dependencies](#25-assumptions-and-dependencies)
3. [Specific Requirements](#3-specific-requirements)
   1. [Functional Requirements](#31-functional-requirements)
   2. [External Interface Requirements](#32-external-interface-requirements)
   3. [System Features](#33-system-features)
   4. [Non-functional Requirements](#34-non-functional-requirements)
4. [Data Management](#4-data-management)
5. [Appendices](#5-appendices)

## 1. Introduction

### 1.1 Purpose

This Software Requirements Specification (SRS) document provides a detailed description of the requirements for the Adept AI Teaching Assistant software. It is intended to be used by AI coding assistants and developers to implement the system according to the user's specifications. This document serves as the foundation for the design, development, testing, and deployment phases of the software development lifecycle.

### 1.2 Scope

The Adept AI Teaching Assistant is a Windows 10 desktop application designed to assist a teacher in lesson planning, resource creation, and classroom instruction. The system will function as an "always-on" voice-activated assistant that can respond to commands, retrieve information, manage files, and interact with students during lessons.

The software will include:
- Voice interaction capabilities (STT and TTS)
- Integration with various AI models as the "brain"
- Access to tools through MCP servers for extended functionality
- Local file system management via a designated "scratchpad" folder
- Class information storage and management
- Google Calendar integration for lesson planning
- Web search capabilities via the Brave Search API

The software is initially intended for use by a single teacher on a personal Windows machine, with potential for future expansion to a commercial solution for wider use.

### 1.3 Definitions, Acronyms, and Abbreviations

- **AI**: Artificial Intelligence
- **API**: Application Programming Interface
- **FSM**: Free School Meals (status indicator for students)
- **GCSE**: General Certificate of Secondary Education
- **IDE**: Integrated Development Environment
- **LLM**: Large Language Model
- **LMS**: Learning Management System
- **MCP**: Model Context Protocol
- **RAG**: Retrieval-Augmented Generation
- **SDLC**: Software Development Lifecycle
- **SEN**: Special Educational Needs
- **SQL**: Structured Query Language
- **SQLite**: A self-contained, serverless, zero-configuration SQL database engine
- **SRS**: Software Requirements Specification
- **STT**: Speech-to-Text
- **SysRS**: System Requirements Specification
- **TTS**: Text-to-Speech
- **UI**: User Interface
- **URS**: User Requirements Specification
- **EAL**: English as an Additional Language
- **H/M/L**: High/Medium/Low (ability grouping)

### 1.4 References

1. fish.audio API Documentation: https://docs.fish.audio/text-to-speech/text-to-speech-ws
2. Model Context Protocol (MCP) Servers: https://github.com/modelcontextprotocol/servers
3. Excel MCP Server: https://github.com/haris-musa/excel-mcp-server
4. Google Calendar MCP: https://github.com/nspady/google-calendar-mcp

### 1.5 Overview

The remainder of this document provides a detailed description of the Adept AI Teaching Assistant software. Section 2 gives an overall description including product perspective, functions, user characteristics, constraints, and assumptions. Section 3 provides specific requirements including functional, external interface, system features, and non-functional requirements. Section 4 covers data management, and Section 5 contains appendices with additional information.

## 2. Overall Description

### 2.1 Product Perspective

The Adept AI Teaching Assistant is a standalone desktop application for Windows 10. It interfaces with several external systems:

1. **Speech Recognition Services**: For converting spoken commands to text
2. **LLM Providers**: For the AI "brain" processing capabilities
3. **TTS Services**: For converting text responses to speech
4. **MCP Servers**: For extended functionality
5. **Local File System**: For storing and retrieving files
6. **Google Calendar**: For lesson scheduling
7. **Brave Search API**: For web queries

The application provides a traditional windowed UI with multiple tabs/sections for configuration, class information management, lesson planning, and system control.

### 2.2 Product Functions

The Adept AI Teaching Assistant will perform the following primary functions:

1. **Voice Recognition and Response**:
   - Listen for the wake word "Adept"
   - Transcribe spoken commands via selected STT service
   - Process commands through the LLM "brain"
   - Convert responses to speech via TTS

2. **Lesson Planning**:
   - Create and manage lesson plans
   - Generate lesson components (retrieval questions, challenge questions, activities)
   - Synchronize lesson plans with Google Calendar

3. **Classroom Assistance**:
   - Track current lesson progress based on time
   - Select students randomly for questioning
   - Evaluate student responses
   - Provide feedback and follow-up questions

4. **Information Retrieval**:
   - Search the web via Brave Search
   - Access and manipulate files in the "scratchpad" folder
   - Process Excel files with class information

5. **System Management**:
   - Configure STT, LLM, and TTS providers
   - Manage API keys
   - Set up class information and teaching schedule
   - Define system prompts for the LLM "brain"

### 2.3 User Characteristics

The primary user is a science teacher with:
- Limited coding knowledge but high technical aptitude
- Experience in physics, chemistry, and biology education
- Teaching responsibilities for students at GCSE and A-level
- Need for time-saving tools for lesson planning and delivery
- Desire for AI integration to enhance teaching effectiveness

### 2.4 Constraints

1. **Hardware Constraints**:
   - Must run on a Windows 10 laptop with 8-16GB RAM
   - Must use AMD Ryzen processor with 8-16 cores
   - Must use the system's default audio input/output devices

2. **Software Constraints**:
   - Must be compatible with Windows 10
   - Must integrate with multiple external APIs
   - Must operate within the limitations of selected AI service providers

3. **Budget Constraints**:
   - Must keep recurring costs relatively low for single-user operation

### 2.5 Assumptions and Dependencies

#### Assumptions:
1. The user has stable internet access for API calls
2. The user has valid API keys for all required services
3. The default audio input device is of sufficient quality for accurate speech recognition
4. The user's teaching schedule follows the specified format (five 1-hour slots per day)
5. The lesson structure remains consistent as specified

#### Dependencies:
1. Availability and reliability of third-party API services:
   - STT providers (Google, OpenAI Whisper)
   - LLM providers (OpenAI, Anthropic, Openrouter, Deepseek, Meta, Google)
   - TTS provider (fish.audio)
   - Brave Search API
   - Google Calendar API

2. Functionality of MCP servers:
   - Filesystem
   - Brave Search
   - Fetch
   - Excel
   - Google Calendar
   - Sequential Thinking
   - Puppeteer

## 3. Specific Requirements

### 3.1 Functional Requirements

#### 3.1.1 Voice Interaction

1. **Wake Word Detection**
   - The system shall continuously monitor audio input from the default microphone
   - The system shall activate and begin processing commands only after detecting the wake word "Adept"
   - The system shall ignore all other audio input when not activated

2. **Speech-to-Text (STT)**
   - The system shall transcribe spoken commands using the selected STT provider
   - The system shall support multiple STT providers including Google and OpenAI Whisper
   - The system shall allow the user to select and configure the preferred STT provider
   - The system shall handle errors in speech recognition gracefully

3. **Large Language Model Processing**
   - The system shall process transcribed commands using the selected LLM "brain"
   - The system shall support multiple LLM providers (OpenAI, Anthropic, Openrouter, Deepseek, Meta, Google)
   - The system shall allow the user to select and configure the preferred LLM provider
   - The system shall apply user-defined system prompts to guide LLM behavior
   - The system shall maintain conversation context for coherent interactions

4. **Text-to-Speech (TTS)**
   - The system shall convert LLM responses to speech using the fish.audio API
   - The system shall use the user's custom voice model from fish.audio
   - The system shall support fallback TTS providers if the primary provider fails
   - The system shall output audio through the default system audio output device

#### 3.1.2 Lesson Planning

1. **Lesson Structure Management**
   - The system shall store and manage lesson structures with the following components:
     - Lesson title
     - Learning objectives
     - Retrieval questions (with answers)
     - Challenge question (with answer)
     - Big question
     - Starter activity
     - Main activity
     - Plenary activity
   - The system shall generate suggestions for each component based on the lesson title and learning objectives
   - The system shall allow the user to accept, modify, or replace suggestions

2. **Teaching Schedule Management**
   - The system shall store the user's weekly teaching schedule
   - The system shall track five daily teaching slots (9am, 10am, 11:30am, 12:30pm, 2pm)
   - The system shall associate classes with specific teaching slots
   - The system shall identify the current lesson based on the time and teaching schedule

3. **Google Calendar Integration**
   - The system shall create calendar events for planned lessons
   - The system shall update calendar events when lesson plans change
   - The system shall retrieve lesson information from calendar events
   - The system shall handle authentication for Google Calendar access

#### 3.1.3 Classroom Assistance

1. **Student Interaction**
   - The system shall randomly select students from the current class when requested
   - The system shall present appropriate questions to selected students based on the current lesson phase
   - The system shall process and evaluate student responses captured via the microphone
   - The system shall provide feedback on student responses
   - The system shall generate follow-up (Socratic) questions to guide students toward correct answers

2. **Lesson Awareness**
   - The system shall track the current time to identify the active lesson
   - The system shall access and utilize the plan for the current lesson
   - The system shall adapt responses based on the current phase of the lesson

#### 3.1.4 Information Management

1. **Class Information Management**
   - The system shall store and manage information for multiple classes
   - The system shall store student information including:
     - Names
     - FSM status
     - SEN status
     - EAL status
     - Ability level (H/M/L)
     - Reading age
     - Target grade
   - The system shall support importing class information from Excel files
   - The system shall allow manual entry and editing of class information

2. **File System Operations**
   - The system shall designate a "scratchpad" folder for file operations
   - The system shall read and process files within the scratchpad folder
   - The system shall create new files in the scratchpad folder when instructed
   - The system shall primarily work with markdown files but support future expansion to other file types

3. **Web Search**
   - The system shall perform web searches using the Brave Search API
   - The system shall retrieve and present relevant information from search results
   - The system shall incorporate search results into responses when appropriate

#### 3.1.5 System Configuration

1. **Provider Selection**
   - The system shall allow the user to select and configure STT providers
   - The system shall allow the user to select and configure LLM providers
   - The system shall allow the user to configure TTS settings
   - The system shall store provider configurations persistently

2. **API Key Management**
   - The system shall prompt for necessary API keys during first-time setup
   - The system shall store API keys in an encrypted format
   - The system shall use stored API keys for service authentication
   - The system shall support updating API keys when needed

3. **System Prompt Configuration**
   - The system shall allow the user to define and store system prompts for the LLM "brain"
   - The system shall apply the configured system prompt to all LLM interactions
   - The system shall support multiple saved system prompts for different contexts

### 3.2 External Interface Requirements

#### 3.2.1 User Interfaces

1. **Main Application Window**
   - The system shall provide a traditional application window with multiple tabs/sections
   - The system shall include tabs/sections for:
     - Configuration (STT, LLM, TTS settings)
     - Class management
     - Lesson planning
     - System status and logs
   - The system shall provide visual feedback for voice activation status
   - The system shall display transcribed commands and responses

2. **Configuration Interfaces**
   - The system shall provide interfaces for selecting and configuring STT providers
   - The system shall provide interfaces for selecting and configuring LLM providers
   - The system shall provide interfaces for configuring TTS settings
   - The system shall provide interfaces for entering and managing API keys
   - The system shall provide interfaces for defining system prompts

3. **Class Management Interface**
   - The system shall provide interfaces for entering and editing class information
   - The system shall provide interfaces for importing class information from Excel files
   - The system shall provide interfaces for configuring the weekly teaching schedule

4. **Lesson Planning Interface**
   - The system shall provide interfaces for creating and editing lesson plans
   - The system shall display suggested lesson components
   - The system shall allow modification of suggested components
   - The system shall show synchronization status with Google Calendar

#### 3.2.2 Hardware Interfaces

1. **Audio Interfaces**
   - The system shall interface with the default system microphone for audio input
   - The system shall interface with the default system audio output for speech output
   - The system shall adapt to changes in default audio devices

#### 3.2.3 Software Interfaces

1. **STT Provider Interfaces**
   - The system shall interface with Google STT API
   - The system shall interface with OpenAI Whisper API
   - The system shall support additional STT providers in the future

2. **LLM Provider Interfaces**
   - The system shall interface with OpenAI API
   - The system shall interface with Anthropic API
   - The system shall interface with Openrouter API
   - The system shall interface with Deepseek API
   - The system shall interface with Meta API
   - The system shall interface with Google API
   - The system shall support additional LLM providers in the future

3. **TTS Provider Interfaces**
   - The system shall interface with fish.audio API via WebSocket
   - The system shall support alternative TTS providers as fallbacks

4. **MCP Server Interfaces**
   - The system shall interface with the Filesystem MCP server
   - The system shall interface with the Brave Search MCP server
   - The system shall interface with the Fetch MCP server
   - The system shall interface with the Excel MCP server
   - The system shall interface with the Google Calendar MCP server
   - The system shall interface with the Sequential Thinking MCP server
   - The system shall interface with the Puppeteer MCP server

5. **Google Calendar Interface**
   - The system shall interface with the Google Calendar API
   - The system shall handle OAuth authentication for Google Calendar
   - The system shall support two-way synchronization with Google Calendar

6. **Database Interface**
   - The system shall interface with a local SQLite database for data persistence
   - The system shall manage database connections efficiently
   - The system shall handle database errors gracefully

### 3.3 System Features

#### 3.3.1 Voice Activation System

1. **Always-On Listening**
   - The system shall maintain an active audio processing thread
   - The system shall perform wake word detection with minimal CPU usage
   - The system shall provide visual indication of listening status

2. **Command Processing Pipeline**
   - The system shall implement a pipeline for:
     - Wake word detection
     - Speech capture
     - STT conversion
     - LLM processing
     - TTS conversion
     - Audio output
   - The system shall process commands asynchronously to prevent UI blocking

#### 3.3.2 MCP Tool Integration

1. **MCP Server Management**
   - The system shall manage connections to MCP servers
   - The system shall initialize MCP servers on application startup
   - The system shall handle MCP server failures gracefully

2. **Tool Routing**
   - The system shall route tool requests from the LLM to appropriate MCP servers
   - The system shall format tool inputs according to MCP server requirements
   - The system shall parse and format tool outputs for LLM consumption

#### 3.3.3 Persistent Memory System

1. **Conversation Memory**
   - The system shall maintain conversation history for each class
   - The system shall store interactions persistently in the database
   - The system shall retrieve relevant conversation history for context

2. **Class Knowledge Base**
   - The system shall maintain a knowledge base of class information
   - The system shall update the knowledge base when class information changes
   - The system shall provide class information to the LLM when relevant

#### 3.3.4 Error Recovery System

1. **Service Failure Handling**
   - The system shall detect failures in external services (STT, LLM, TTS)
   - The system shall attempt to reconnect to failed services
   - The system shall fall back to alternative providers when appropriate
   - The system shall notify the user of persistent failures

2. **Error Logging**
   - The system shall log all errors with relevant context
   - The system shall categorize errors by severity
   - The system shall provide error logs for troubleshooting

#### 3.3.5 Installation and Setup

1. **Installation Wizard**
   - The system shall provide a Windows installation wizard
   - The system shall install all required components including MCP servers
   - The system shall guide the user through initial configuration

2. **First-Run Setup**
   - The system shall detect first-run status
   - The system shall prompt for necessary API keys
   - The system shall guide the user through basic configuration
   - The system shall verify connectivity to all required services

### 3.4 Non-functional Requirements

#### 3.4.1 Performance Requirements

1. **Response Time**
   - The system shall activate within 1 second of hearing the wake word
   - The system shall transcribe speech within 2 seconds of command completion
   - The system shall generate LLM responses within 5 seconds for typical queries
   - The system shall begin TTS output within 1 second of LLM response completion

2. **Resource Usage**
   - The system shall use less than 20% CPU during idle listening
   - The system shall use less than 1GB of RAM during normal operation
   - The system shall scale resource usage appropriately during intensive tasks

#### 3.4.2 Security Requirements

1. **Data Protection**
   - The system shall encrypt all stored API keys
   - The system shall encrypt sensitive student information
   - The system shall not transmit sensitive information to unauthorized services

2. **Authentication**
   - The system shall securely authenticate with all external services
   - The system shall handle expired tokens and re-authentication

#### 3.4.3 Reliability Requirements

1. **Stability**
   - The system shall maintain continuous operation during teaching sessions
   - The system shall handle unexpected errors without crashing
   - The system shall recover from component failures when possible

2. **Data Integrity**
   - The system shall prevent data corruption in the local database
   - The system shall implement database transaction safety
   - The system shall backup configuration data periodically

#### 3.4.4 Maintainability Requirements

1. **Code Organization**
   - The system shall use a modular architecture
   - The system shall separate concerns appropriately
   - The system shall implement clean interfaces between components

2. **Configuration Management**
   - The system shall store configuration in accessible formats
   - The system shall support configuration export and import
   - The system shall provide a reset-to-defaults option

#### 3.4.5 Usability Requirements

1. **Ease of Use**
   - The system shall provide intuitive interfaces for all functions
   - The system shall use consistent UI patterns across the application
   - The system shall provide tooltips and help text for complex features

2. **Accessibility**
   - The system shall support keyboard navigation
   - The system shall use high-contrast UI elements where appropriate
   - The system shall provide visual alternatives to audio cues

## 4. Data Management

### 4.1 Database Schema

The SQLite database shall include the following tables:

1. **Configuration**
   - API keys (encrypted)
   - Provider preferences
   - System settings

2. **Classes**
   - Class ID
   - Class code
   - Education level
   - Current topic
   - Associated teaching slots

3. **Students**
   - Student ID
   - Class ID (foreign key)
   - Name
   - FSM status
   - SEN status
   - EAL status
   - Ability level
   - Reading age
   - Target grade

4. **TeachingSchedule**
   - Day of week
   - Time slot
   - Class ID (foreign key)

5. **LessonPlans**
   - Lesson ID
   - Class ID (foreign key)
   - Date and time
   - Title
   - Learning objectives
   - Calendar event ID
   - Components (JSON)

6. **Conversations**
   - Conversation ID
   - Class ID (foreign key)
   - Date and time
   - Interaction history (JSON)

7. **SystemPrompts**
   - Prompt ID
   - Name
   - Content
   - Is default (boolean)

### 4.2 File System Structure

The application shall organize files as follows:

1. **Application Folder**
   - Executable files
   - Configuration files
   - Database file

2. **Scratchpad Folder**
   - User-designated location
   - Read/write access for the application
   - May contain subfolders

## 5. Appendices

### 5.1 Glossary

- **Lesson Structure**: The standardized format for lessons including title, objectives, retrieval questions, challenge question, big question, starter activity, main activity, and plenary activity.
- **Scratchpad**: The designated folder where the AI assistant has read/write access for file operations.
- **Wake Word**: The specific word ("Adept") that activates the voice recognition system.
- **MCP Server**: Model Context Protocol server that provides specific tools and capabilities to the LLM "brain".

### 5.2 References

1. Windows 10 Development Guidelines
2. SQLite Best Practices
3. API Documentation for all integrated services
4. MCP Server Documentation

### 5.3 Future Extensions

Potential future extensions to be considered in the design:

1. **LlamaParse Integration**: For parsing and embedding student data and curriculum resources
2. **LightRAG System**: For efficient retrieval of relevant information
3. **Student Assessment Analytics**: For ingesting and analyzing student performance data
4. **Extended Computer Control**: For launching applications and controlling presentations
5. **Web Application Conversion**: For providing the service to other teachers

---

This SRS document provides a comprehensive and detailed specification for the Adept AI Teaching Assistant. It will serve as the foundation for the design, development, testing, and deployment of the system.