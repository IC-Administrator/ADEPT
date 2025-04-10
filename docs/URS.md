# User Requirements Specification (URS)

## For Adept AI Teaching Assistant

**Version 1.0**  
**Date: April 8, 2025**

## Table of Contents

1. [Introduction](#1-introduction)
   1. [Purpose](#11-purpose)
   2. [Intended Audience](#12-intended-audience)
   3. [Scope](#13-scope)
2. [User Characteristics](#2-user-characteristics)
3. [User Stories](#3-user-stories)
   1. [Voice Interaction](#31-voice-interaction)
   2. [Lesson Planning](#32-lesson-planning)
   3. [Classroom Assistance](#33-classroom-assistance)
   4. [Information Management](#34-information-management)
   5. [System Configuration](#35-system-configuration)
4. [User Interface Requirements](#4-user-interface-requirements)
5. [Performance Expectations](#5-performance-expectations)
6. [Usability Requirements](#6-usability-requirements)
7. [Constraints and Assumptions](#7-constraints-and-assumptions)

## 1. Introduction

### 1.1 Purpose

This User Requirements Specification (URS) document captures the needs, expectations, and requirements of the primary user (a science teacher) for the Adept AI Teaching Assistant. It describes what the user needs to accomplish with the system and why these capabilities are important from the user's perspective.

### 1.2 Intended Audience

This document is intended for:
- AI coding assistants who will implement the system
- Developers who will be involved in the project
- The user (science teacher) to validate that requirements have been accurately captured

### 1.3 Scope

This URS covers all aspects of the Adept AI Teaching Assistant from the user's perspective, including:
- Voice-activated assistance during lessons
- Lesson planning and resource creation
- Student interaction and assessment
- Class information management
- System configuration and customization

The requirements expressed in this document focus on the initial implementation for a single user, with consideration for potential future expansion to a commercial solution.

## 2. User Characteristics

The primary user of the Adept AI Teaching Assistant is a science teacher with the following characteristics:

- A high school science teacher based in the UK
- Developer of interactive online courses for GCSE and A-level students in physics, chemistry, and biology
- Creates content that exceeds standard curriculum requirements to prepare students for higher education
- Limited coding knowledge but currently learning Python, JavaScript, TypeScript, HTML, CSS, and SQL
- Quick learner with strong problem-solving skills
- Teaches multiple classes across a structured weekly schedule
- Follows a consistent lesson structure across all classes
- Requires assistance with lesson planning, resource creation, and classroom management
- Values efficiency, time-saving, and enhanced student engagement

## 3. User Stories

### 3.1 Voice Interaction

1. **Wake Word Detection**
   - As a teacher, I want Adept to respond only when I specifically call its name, so that it doesn't interrupt my teaching with false activations.
   - As a teacher, I want Adept to be always listening but only activate when needed, so that I don't have to manually trigger the system during lessons.

2. **Speech Recognition**
   - As a teacher, I want Adept to accurately transcribe my spoken commands, so that I don't have to repeat myself or correct misunderstandings.
   - As a teacher, I want to be able to select different speech recognition providers, so that I can choose the most accurate or cost-effective option for my needs.

3. **AI Response Processing**
   - As a teacher, I want Adept to understand complex educational instructions and context, so that it can provide appropriate assistance during lessons.
   - As a teacher, I want to customize how Adept responds through system prompts, so that I can adjust its behavior to suit different teaching situations.

4. **Voice Output**
   - As a teacher, I want Adept to respond using my custom voice model, so that the voice sounds natural and consistent to my students.
   - As a teacher, I want Adept's voice responses to be clear and appropriately paced for classroom settings, so that students can easily understand the information being provided.

### 3.2 Lesson Planning

1. **Lesson Structure Management**
   - As a teacher, I want Adept to help me create structured lesson plans with all required components, so that I can maintain consistent high-quality lessons.
   - As a teacher, I want Adept to suggest appropriate retrieval questions, challenge questions, big questions, and activities based on lesson topics, so that I can save time on lesson preparation.
   - As a teacher, I want to be able to modify Adept's suggestions, so that I can customize plans to fit my teaching style and specific class needs.

2. **Teaching Schedule Management**
   - As a teacher, I want to set up my weekly teaching schedule in Adept, so that it knows which classes I'm teaching and when.
   - As a teacher, I want Adept to be aware of the current lesson based on the time, so that it automatically provides relevant information and assistance.

3. **Google Calendar Integration**
   - As a teacher, I want Adept to add my lesson plans to Google Calendar, so that I can access them from any device.
   - As a teacher, I want changes to my lesson plans to be synchronized with Google Calendar, so that my schedule remains up-to-date.
   - As a teacher, I want Adept to read lesson details from calendar events, so that I don't have to manually re-enter information.

### 3.3 Classroom Assistance

1. **Student Interaction**
   - As a teacher, I want Adept to randomly select students for questioning, so that I can ensure fair participation across the class.
   - As a teacher, I want Adept to present appropriate questions to selected students based on the current lesson phase, so that questions align with what we're currently learning.
   - As a teacher, I want Adept to evaluate student responses and provide feedback, so that I can assess understanding in real-time.
   - As a teacher, I want Adept to generate Socratic follow-up questions to guide students toward correct answers, so that students develop critical thinking skills.

2. **Lesson Awareness**
   - As a teacher, I want Adept to know which lesson is currently in progress, so that it can provide context-relevant assistance.
   - As a teacher, I want Adept to track the current phase of the lesson (retrieval questions, starter activity, main activity, etc.), so that it can adapt its support appropriately.
   - As a teacher, I want Adept to have access to all planned lesson materials, so that it can reference them during class.

### 3.4 Information Management

1. **Class Information Management**
   - As a teacher, I want to store information about my classes including student names and educational data, so that Adept can personalize interactions with students.
   - As a teacher, I want to import class information from Excel files, so that I don't have to manually enter data that already exists in school records.
   - As a teacher, I want to be able to update class information when needed, so that Adept always has the most current data.

2. **File System Operations**
   - As a teacher, I want Adept to have a designated "scratchpad" folder for file operations, so that it can access and create files without affecting other parts of my system.
   - As a teacher, I want Adept to read and use documents I've created as context for its responses, so that it can incorporate my teaching materials.
   - As a teacher, I want Adept to create new documents based on my instructions, so that it can help generate teaching resources.

3. **Web Search**
   - As a teacher, I want Adept to search the web for information when needed, so that it can provide up-to-date facts and resources.
   - As a teacher, I want Adept to incorporate web search results into its responses, so that it can supplement its knowledge with current information.

### 3.5 System Configuration

1. **Provider Selection**
   - As a teacher, I want to select and configure different STT, LLM, and TTS providers, so that I can optimize for performance, cost, or specific features.
   - As a teacher, I want my provider selections to persist between sessions, so that I don't have to reconfigure the system each time I use it.

2. **API Key Management**
   - As a teacher, I want to securely store my API keys, so that I don't have to re-enter them each session.
   - As a teacher, I want my stored API keys to be encrypted, so that my account credentials remain secure.

3. **System Prompt Configuration**
   - As a teacher, I want to define and save custom system prompts, so that I can control how Adept behaves in different contexts.
   - As a teacher, I want to switch between different saved system prompts, so that I can quickly adapt Adept for different teaching situations.

## 4. User Interface Requirements

1. **Application Layout**
   - As a teacher, I want a traditional application window with multiple tabs/sections, so that I can easily access different functions of the system.
   - As a teacher, I want clear visual organization of features, so that I can find what I need quickly during busy teaching periods.

2. **Configuration Interfaces**
   - As a teacher, I want intuitive interfaces for selecting and configuring STT, LLM, and TTS providers, so that I can make changes without technical expertise.
   - As a teacher, I want a secure method for entering and managing API keys, so that I can maintain access to required services.
   - As a teacher, I want a simple interface for creating and editing system prompts, so that I can customize Adept's behavior as needed.

3. **Class Management Interface**
   - As a teacher, I want a clear interface for entering and editing class information, so that I can maintain accurate student data.
   - As a teacher, I want a simple method for importing class information from Excel files, so that I can quickly set up new classes.
   - As a teacher, I want an intuitive way to configure my weekly teaching schedule, so that Adept knows which classes I'm teaching and when.

4. **Lesson Planning Interface**
   - As a teacher, I want a structured interface for creating and editing lesson plans, so that I can ensure all required components are included.
   - As a teacher, I want to see Adept's suggested lesson components clearly displayed, so that I can review and modify them as needed.
   - As a teacher, I want visual confirmation of synchronization with Google Calendar, so that I know my lesson plans are being saved properly.

5. **Voice Interaction Feedback**
   - As a teacher, I want clear visual feedback when Adept is listening, so that I know when it's ready for commands.
   - As a teacher, I want to see transcribed commands and responses, so that I can verify accuracy and refer back to previous interactions.

## 5. Performance Expectations

1. **Response Time**
   - As a teacher, I want Adept to activate quickly after hearing the wake word, so that there's minimal delay in getting assistance.
   - As a teacher, I want transcription, LLM processing, and TTS conversion to happen with minimal delay, so that interactions feel natural and don't disrupt the flow of the lesson.

2. **Reliability**
   - As a teacher, I want Adept to maintain continuous operation during teaching sessions, so that I can rely on it throughout a lesson without interruptions.
   - As a teacher, I want Adept to handle errors gracefully and recover from failures, so that minor issues don't prevent me from using the system.

3. **Error Handling**
   - As a teacher, I want Adept to handle errors with minimal disruption, silently retrying operations when possible.
   - As a teacher, I want to be notified of persistent errors that affect functionality, so that I can take appropriate action.
   - As a teacher, I want clear guidance on resolving common issues, so that I can quickly address problems that arise.

## 6. Usability Requirements

1. **Ease of Use**
   - As a teacher, I want Adept to be intuitive and straightforward to use, so that I can operate it with minimal training.
   - As a teacher, I want clear, simple commands for all functions, so that I can interact with Adept naturally during lessons.

2. **Learnability**
   - As a teacher, I want helpful tooltips and instructions, so that I can quickly learn how to use all features.
   - As a teacher, I want a smooth learning curve, with basic functions being immediately accessible and advanced features being discoverable as needed.

3. **Efficiency**
   - As a teacher, I want streamlined workflows for common tasks, so that I can accomplish them quickly.
   - As a teacher, I want keyboard shortcuts for frequent actions, so that I can operate the system efficiently.
   - As a teacher, I want to save and reuse configurations and templates, so that I don't have to recreate common items.

## 7. Constraints and Assumptions

1. **Technical Constraints**
   - The system must run on a Windows 10 laptop with 8-16GB RAM and 8-16 cores AMD Ryzen processor.
   - The system must use the default Windows audio input/output devices.
   - The system must integrate with multiple external APIs requiring appropriate permissions and authentication.

2. **Environmental Constraints**
   - The system will be used in a classroom environment where ambient noise may affect audio recognition.
   - Internet connectivity must be available for API access during operation.

3. **Knowledge Constraints**
   - The user has limited coding knowledge but is capable of following technical instructions.
   - The user is familiar with educational concepts and terminology specific to UK GCSE and A-level science curricula.

4. **Assumptions**
   - The user has valid API keys for all required services.
   - The user has a Google account and permissions to access Google Calendar API.
   - The user's teaching schedule follows the specified format (five 1-hour slots per day).
   - The lesson structure remains consistent as specified.
   - The user has sufficient disk space for the application and associated data storage.

---

This URS document captures the requirements for the Adept AI Teaching Assistant from the user's perspective, focusing on what the user needs to accomplish and why these capabilities are important. It serves as a foundation for system design and development, ensuring that the resulting application meets the specific needs of a science teacher in a classroom environment.