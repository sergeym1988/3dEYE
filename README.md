## Overview

This project contains two main Web API services:

1. **File Generator API** — generates large text files with random data.  
2. **File Sorter API** — sorts generated files by a custom key.

There is also a **Common** project shared by both APIs.

---

## How It Works

- When you request file generation, the API starts the process and immediately returns a unique file ID.  
- You can check the status of the file generation using this file ID.  
- After the file is generated, you can request the sorting of that file by its ID.  
- Sorting also works asynchronously — you start the job and can check its status.

---

## Important Notes

- **Asynchronous Processing:**  
  In this demo implementation, file generation and sorting tasks run as background `Task.Run` within the API process for simplicity. This approach works for testing and small loads but is not suitable for production.

- **Production Recommendation:**  
  For real-world usage, it's recommended to use a dedicated task queue system (e.g., RabbitMQ, Azure Queue, Hangfire) to manage background jobs reliably and scale better.

- **Status Checking:**  
  Both APIs provide endpoints to check the status of tasks by file ID.

- **File Size Limits:**  
  The API enforces maximum file size limits to avoid resource exhaustion.

---

## Configuration

- Default output directories are configured in `appsettings.json` or via options classes.  
- Maximum file size limits are enforced in the API to prevent excessive resource use.  
- Chunk size for file generation and sorting is configurable.

---

## Running Locally

1. Clone the repository.  
2. Build the solution.  
3. Run the APIs using your preferred IDE or command line.

---

### File Generator API

- `POST /api/generator`  
  Start generating a file. Request body: `{ "fileSizeInMb": <number> }`  
  Response: `{ "fileId": "<guid>" }`

- `GET /api/generator/status/{fileId}`  
  Check status of file generation.

---

### File Sorter API

- `POST /api/sorter/{fileId}`  
  Start sorting the generated file.

- `GET /api/sorter/status/{fileId}`  
  Check status of sorting.

---

## Testing

- Unit tests cover key logic of file generation and sorting handlers.
