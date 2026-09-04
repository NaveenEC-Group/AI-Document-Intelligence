# AI Document Intelligence — Angular Frontend

Angular 19 UI for uploading PDFs and viewing structured extraction results from the .NET API.

## Run

1. Start the API (from the repo root):

```bash
dotnet run --project "AI Document Intelligence.csproj" --launch-profile http
```

2. Start the frontend:

```bash
cd frontend
npm start
```

3. Open http://localhost:4200

API base URL: `http://localhost:5017` (see `src/environments/environment.development.ts`).

## Features

- Drag-and-drop or browse PDF upload
- Calls `POST /api/documents/upload` then `GET /api/documents/{id}`
- Shows vendor, customer, invoice fields, and raw JSON
