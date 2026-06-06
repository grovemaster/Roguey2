#!/usr/bin/env python3
"""Generate Unity .meta files for NPC/portrait PNG sprites."""
import uuid
import os

BASE = "/home/jonathan/UnityProjects/Roguey2/Assets/Art"

TEXTURE_META = """fileFormatVersion: 2
guid: {guid}
TextureImporter:
  internalIDToNameTable: []
  externalObjects: {{}}
  serializedVersion: 13
  mipmaps:
    mipMapMode: 0
    enableMipMap: 0
    sRGBTexture: 1
    linearTexture: 0
    fadeOut: 0
    borderMipMap: 0
    mipMapsPreserveCoverage: 0
    alphaTestReferenceValue: 0.5
    mipMapFadeDistanceStart: 1
    mipMapFadeDistanceEnd: 3
  bumpmap:
    convertToNormalMap: 0
    externalNormalMap: 0
    heightScale: 0.25
    normalMapFilter: 0
    flipGreenChannel: 0
  isReadable: 0
  streamingMipmaps: 0
  streamingMipmapsPriority: 0
  vTOnly: 0
  ignoreMipmapLimit: 0
  grayScaleToAlpha: 0
  generateCubemap: 6
  cubemapConvolution: 0
  seamlessCubemap: 0
  textureFormat: 1
  maxTextureSize: 2048
  textureSettings:
    serializedVersion: 2
    filterMode: 0
    aniso: 1
    mipBias: 0
    wrapU: 0
    wrapV: 0
    wrapW: 0
  nPOTScale: 0
  lightmap: 0
  compressionQuality: 50
  spriteMode: 1
  spriteExtrude: 1
  spriteMeshType: 1
  alignment: 0
  spritePivot: {{x: 0.5, y: {pivot_y}}}
  spritePixelsToUnits: {ppu}
  spriteBorder: {{x: 0, y: 0, z: 0, w: 0}}
  spriteGenerateFallbackPhysicsShape: 1
  alphaUsage: 1
  alphaIsTransparency: 1
  spriteTessellationDetail: -1
  textureType: 8
  textureShape: 1
  singleChannelComponent: 0
  flipbookRows: 1
  flipbookColumns: 1
  maxTextureSizeSet: 0
  compressionQualitySet: 0
  textureFormatSet: 0
  ignorePngGamma: 0
  applyGammaDecoding: 1
  swizzle: 50462976
  cookieLightType: 1
  platformSettings:
  - serializedVersion: 4
    buildTarget: DefaultTexturePlatform
    maxTextureSize: 2048
    resizeAlgorithm: 0
    textureFormat: -1
    textureCompression: 0
    compressionQuality: 50
    crunchedCompression: 0
    allowsAlphaSplitting: 0
    overridden: 0
    ignorePlatformSupport: 0
    androidETC2FallbackOverride: 0
    forceMaximumCompressionQuality_BC6H_BC7: 0
  spriteSheet:
    serializedVersion: 2
    sprites: []
    outline: []
    customData: 
    physicsShape: []
    bones: []
    spriteID: 5e97eb03825dee720800000000000000
    internalID: 0
    vertices: []
    indices: 
    edges: []
    weights: []
    secondaryTextures: []
    spriteCustomMetadata:
      entries: []
    nameFileIdTable: {{}}
  mipmapLimitGroupName: 
  pSDRemoveMatte: 0
  userData: 
  assetBundleName: 
  assetBundleVariant: 
"""

FOLDER_META = """fileFormatVersion: 2
guid: {guid}
folderAsset: yes
DefaultImporter:
  externalObjects: {{}}
  userData: 
  assetBundleName: 
  assetBundleVariant: 
"""

def write_meta(path, content):
    with open(path + ".meta", "w") as f:
        f.write(content)

files = [
    ("Art/NPC/Sprites/NPC_Mira.png", 32, "0.25"),
    ("Art/NPC/Sprites/NPC_Luc.png", 32, "0.25"),
    ("Art/NPC/Sprites/NPC_Edda.png", 32, "0.25"),
    ("Art/Portraits/NPC/Portrait_Mira.png", 128, "0.5"),
    ("Art/Portraits/NPC/Portrait_Luc.png", 128, "0.5"),
    ("Art/Portraits/NPC/Portrait_Edda.png", 128, "0.5"),
    ("Art/Portraits/Party/Race/Portrait_Human.png", 128, "0.5"),
    ("Art/Portraits/Party/Race/Portrait_Barbarian.png", 128, "0.5"),
    ("Art/Portraits/Party/Race/Portrait_Elf.png", 128, "0.5"),
]

for rel, ppu, pivot_y in files:
    full = os.path.join(BASE, rel.replace("Art/", ""))
    if not os.path.exists(full):
        print("missing", full)
        continue
    if os.path.exists(full + ".meta"):
        continue
    write_meta(full, TEXTURE_META.format(guid=uuid.uuid4().hex, ppu=ppu, pivot_y=pivot_y))

folders = [
    "Art/NPC/Sprites",
    "Art/Portraits/NPC",
    "Art/Portraits/Party",
    "Art/Portraits/Party/Race",
]
for rel in folders:
    full = os.path.join("/home/jonathan/UnityProjects/Roguey2", rel)
    os.makedirs(full, exist_ok=True)
    if not os.path.exists(full + ".meta"):
        write_meta(full, FOLDER_META.format(guid=uuid.uuid4().hex))

print("done")
