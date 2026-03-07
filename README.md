# Customer-API | Crowdsourced Ideation Solution (CIS) - Phase 1: User Management API

## 1. Introduction
This repository contains **Phase 1** of the CIS project. The main objective is the transition from a CLI-based employee management system to a modern service-oriented architecture (**API-First**).

This phase establishes a **User Management API** as the single source of truth, allowing coexistence with the legacy system without disrupting current operations.
---
## 2. Hybrid Technology Stack
In accordance with architectural decisions and academic guidelines, the project uses a diversified stack:
* **Main Backend (API):** Java with **Spring Boot** for service logic and persistence.

* **Integration/UI Components:** **C#/.NET** with Avalonia UI for administrative tools.

* **Database:** **MySQL** (shared RDBMS) managed using **Docker**.

* **Architecture:** RESTful API based on the HTTP standard.

---

## 3. System Architecture (C4 Diagrams)

### 3.1 Level 1: Context
The core user system interacts with Administrators and future Clients, maintaining data integrity with the Legacy CLI through the shared database.

### 3.2 Level 2: Containers
Shows the coexistence of Spring Boot (Java) containers and .NET components accessing the same MySQL instance.
---

## 4. Development Strategy: API-First
We adopt an **API-First** approach where the programming interface is central to the design.

* **Definition:** APIs are rules that enable data communication between programs.

* **Prioritization:** By making the API the core component, we facilitate collaboration between the Java and C# teams, enabling component reuse.

* **Benefit:** Ensures that all parties involved work on a standardized data contract before implementation.

---

## 5. Workflow and Code Quality Model
### Branching Model (GitFlow)
We follow the GitFlow model to maintain code traceability and stability:
* **main**: Stable production code.

* **develop**: Continuous integration branch.

* **feature/issue-ID-description**: Development of specific tasks linked to an issue.

### Code Review Process
* **Requirement:** Minimum **1 peer approval** to perform the merge.

* **Restriction:** The author cannot approve their own Merge Request (MR).

* **Linking:** Every MR must include the `Closes #ID` instruction to automatically close the associated issue.

---

## 6. Technical Documentation (ADR)
Key decisions are documented in `/docs/adr/`:
* **ADR-001:** Initial Persistence Strategy (MySQL + Docker).

* **ADR-002:** Technology Stack Selection (Java/Spring Boot & .NET).

* **ADR-003:** REST Communication Style.

---

## 7. Development Environment Setup
1. **Clone:** `git clone [REPO-URL]`
2. **Docker:** Start the MySQL container: `docker-compose up -d`.

3. **Database:** Import the `schema.sql` script into the local MySQL instance.

4. **Environments:** Configure the JDK for Spring Boot and the .NET SDK for the C# components.
