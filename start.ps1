if (-not (Test-Path .env) -and (Test-Path .env.example)) {
    Copy-Item .env.example .env
}

docker compose up --build
