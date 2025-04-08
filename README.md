# ADEPT AI Teaching Assistant

## Overview
ADEPT (AI Teaching Assistant) is a Windows 10 desktop application designed to assist teachers, particularly science teachers, with lesson planning, resource creation, and classroom instruction. The application functions as an "always-on" voice-activated assistant that can respond to commands, retrieve information, manage files, and interact with students during lessons.

## Key Features

### Voice Interaction
- Wake word detection ("Adept")
- Speech-to-Text (STT) processing
- Text-to-Speech (TTS) conversion
- Visual feedback for voice status

### AI Integration
- Multiple LLM providers (OpenAI, Anthropic, Openrouter, Deepseek, Meta, Google)
- Context management for conversations
- System prompt handling
- Tool integration for extended functionality

### Tool Integration (MCP Servers)
- Filesystem operations in a designated "scratchpad" folder
- Brave Search for web queries
- Google Calendar integration for lesson scheduling
- Excel processing for class information

### Lesson Planning
- Structured lesson creation with standardized components
- Generation of retrieval questions, challenge questions, activities
- Google Calendar synchronization
- Template management

### Class Information Management
- Student data storage (FSM, SEN, EAL status, ability levels, etc.)
- Class scheduling
- Excel import capabilities

## Technical Stack
- **Language**: C# with .NET 6.0+
- **UI Framework**: WPF (Windows Presentation Foundation)
- **Database**: SQLite with Microsoft.Data.Sqlite
- **Architecture Pattern**: Modified MVVM

## Project Status
This project is currently in the design and planning phase. The repository contains comprehensive documentation:
- Software Requirements Specification (SRS)
- User Requirements Specification (URS)
- System Requirements Specification (SysRS)
- Design Document

Implementation will begin soon.

## Future Extensions
- LlamaParse Integration for document processing
- LightRAG System for efficient information retrieval
- Student Assessment Analytics
- Extended Computer Control
- Web Application Conversion
