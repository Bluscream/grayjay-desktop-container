# Grayjay Desktop Container

Run a Dockerized version of the [Grayjay Desktop](https://grayjay.app/desktop/) application in server mode.

Currently, this only builds `linux/amd64` images.  This means that, for a host such as macOS running on Apple Silicon, you will need to enable a virtualization layer for Docker such as Rosetta 2.


## Features

- Run the GrayJay.Desktop application in server (browser) mode
- Specify a custom port number (currently [not possible](https://github.com/futo-org/Grayjay.Desktop/issues/708) in official native builds)
- Easily spin up multiple instances with their own data directories
- Make GrayJay.Desktop your own with custom patches
- Example `docker-compose.yml` file included to demonstrate orchestration


## Getting Started

1. Clone this repo:

```sh
git clone "https://github.com/ravenstine/grayjay-desktop-container.git"
cd grayjay-desktop-container
```

2. Pull the Grayjay.Desktop submodules (this might take a few minutes):

```sh
git submodule update --init --recursive --remote
```

3. Pull the necessary LFS files:

```sh
git lfs pull --include="vendor/Grayjay.Desktop/JustCef/prebuilt/linux-x64/**,vendor/Grayjay.Desktop/Grayjay.ClientServer/deps/linux-x64/**"
```

4. Apply the patches:

```sh
./script/patch
```

5. Build the image:

```sh
docker compose build
```

6. Run a container:

```sh
docker compose up
```

By default, the application should be available to the host at `http://localhost:11338`.  The provided `docker-compose.yml` specifies that the runtime data for the application is stores in a volume at `data/` in the repo's directory.


## Casting

Casting won't be as easy on a non-Linux host because of the lack of exposure to mDNS from within the VM.  It should still work, but you will need to manually enter an IP address into the application for your AirPlay or Chromecast device.


## Development

Any modifications to the Grayjay.Desktop code are managed with Git patch files that live in the `patches/` directory.

To create your own patches:

1. Modify the code in `vendor/Grayjay.Desktop`.
2. Create a patch for your changes:

```sh
git -C vendor/Grayjay.Desktop diff > "patches/000{number here}-{name here}.patch"
```

or

```sh
git -C vendor/Grayjay.Desktop ls-files --others --exclude-standard -z | xargs -0 -n 1 git -C vendor/Grayjay.Desktop --no-pager diff /dev/null | less > patches/patches/000{number here}-{name here}.patch.patch
```

3. Commit those patches to this repo.


## Publishing to Docker Hub and GitHub Container Registry

This repository includes a GitHub Actions workflow that automatically builds and publishes Docker images to both Docker Hub and GitHub Container Registry (ghcr.io).

### Setup

To enable automatic publishing, you need to configure the following secrets in your GitHub repository:

1. **Docker Hub Secrets** (Settings → Secrets and variables → Actions):
   - `DOCKERHUB_USERNAME`: Your Docker Hub username
   - `DOCKERHUB_TOKEN`: Your Docker Hub access token (create one at https://hub.docker.com/settings/security)

2. **GitHub Container Registry**: No additional setup needed - uses the built-in `GITHUB_TOKEN`

### Automatic Publishing

The workflow automatically publishes images when:
- Pushing to `main` or `master` branch (tags as `latest` and branch name)
- Creating a git tag starting with `v` (e.g., `v1.0.0`)
- Manually triggering via GitHub Actions UI (workflow_dispatch)

### Image Locations

After publishing, images will be available at:
- **Docker Hub**: `docker.io/<DOCKERHUB_USERNAME>/grayjay-desktop-container`
- **GitHub Container Registry**: `ghcr.io/<GITHUB_USERNAME>/grayjay-desktop-container`

### Pulling Published Images

```sh
# From Docker Hub
docker pull <DOCKERHUB_USERNAME>/grayjay-desktop-container:latest

# From GitHub Container Registry
docker pull ghcr.io/<GITHUB_USERNAME>/grayjay-desktop-container:latest
```

Note: For GHCR, you may need to authenticate first:
```sh
echo $GITHUB_TOKEN | docker login ghcr.io -u <GITHUB_USERNAME> --password-stdin
```

## Developer Notes

The code under `src/Grayjay.Desktop.Server` might seem redundant, but it exists so that we can build the application without the Chromium Embedded Framework (CEF) dependencies that are completely unnecessary for the purpose of the resulting Docker image.  Incidentally, it makes building the image significantly faster and less resource-intensive.
