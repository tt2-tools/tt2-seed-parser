import os
import discord
from discord import app_commands

intents = discord.Intents.default()
intents.message_content = True
client = discord.Client(intents=intents)

TARGET_CHANNEL_ID = int(os.environ["TARGET_CHANNEL_ID"])
DATA_DIR = "/data"
PROCESSED_REACTION = "👍"
tree = app_commands.CommandTree(client)


async def process_seed_message(message):
    already_processed = any(
        reaction.me and str(reaction.emoji) == PROCESSED_REACTION
        for reaction in message.reactions
    )
    if already_processed:
        return
    for attachment in message.attachments:
        if attachment.filename.endswith(".json"):
            dest = os.path.join(DATA_DIR, attachment.filename)
            await attachment.save(dest)
            print(f"Seed file saved: {attachment.filename}")
    await message.add_reaction(PROCESSED_REACTION)


@client.event
async def on_message(message):
    if message.channel.id != TARGET_CHANNEL_ID:
        return
    await process_seed_message(message)


@client.event
async def on_ready():
    await tree.sync()
    channel = await client.fetch_channel(TARGET_CHANNEL_ID)
    async for message in channel.history(limit=None, oldest_first=True):
        await process_seed_message(message)
    print(f"Bot connecté en tant que {client.user}")


client.run(os.environ["DISCORD_TOKEN"])
