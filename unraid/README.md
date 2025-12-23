# Unraid Template for Grayjay Desktop Container

This folder contains the Unraid Docker template for Grayjay Desktop Container.

## Installation

1. Copy the `grayjay-desktop-container.xml` file to your Unraid server
2. Place it in `/boot/config/plugins/dockerMan/templates-user/` (or use the Community Applications plugin)
3. Go to Docker → Add Container in Unraid
4. Select "Grayjay Desktop Container" from the template dropdown
5. Configure the ports and data directory as needed
6. Click Apply to start the container

## Configuration

- **WebUI Port**: Port for accessing the Grayjay web interface (default: 11338)
- **HTTP Proxy Port**: Port for the HTTP proxy service (default: 11339)
- **Casting Port**: Port for casting helper service (default: 11340)
- **Data Directory**: Directory to store Grayjay application data (default: `/mnt/user/appdata/grayjay`)

## Access

Once the container is running, access the Grayjay web interface at:
```
http://[YOUR_UNRAID_IP]:11338
```

## Alternative Image Sources

You can also use the GitHub Container Registry image by changing the repository in the template:
```
ghcr.io/bluscream/grayjay-desktop-container:latest
```

## Support

For issues, feature requests, or contributions, please visit:
https://github.com/Bluscream/grayjay-desktop-container

