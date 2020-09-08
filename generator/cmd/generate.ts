import process from 'process';
import os from 'os';
import path from 'path';
import fs from 'fs';
import { series } from 'async';
import { promisify } from 'util';
import { spawn } from 'child_process';
import chalk from 'chalk';

const readdir = promisify(fs.readdir);
const stat = promisify(fs.stat);

const inputBaseDir = path.resolve(`${__dirname}/../../../azure-rest-api-specs`);
const outputBaseDir = path.resolve(`${__dirname}/../../library/Bicep.Types.Arm/generated`);
const extensionDir = path.resolve(`${__dirname}/../`);
const autorestBinary = os.platform() === 'win32' ? 'autorest.cmd' : 'autorest';
const autorestCoreVersion = '3.0.6306';

executeSynchronous(async () => {
  const readmePaths = await findReadmePaths(inputBaseDir);
  for (const path of readmePaths) {
    try {
      await generateSchema(path, outputBaseDir);
    } catch (e) {
      console.log(e);
    }
  }
});

async function generateSchema(readme: string, outputBaseDir: string) {
  const autoRestParams = [
    // `--azureresourceschema.debugger=true`, // uncomment for debugging
    `--version=${autorestCoreVersion}`,
    `--use=${extensionDir}`,
    '--azureresourceschema',
    `--output-folder=${outputBaseDir}`,
    `--multiapi`,
    '--title=none',
    '--pass-thru:subset-reducer',
    readme,
  ];

  return await executeCmd(__dirname, autorestBinary, autoRestParams);
}

async function findReadmePaths(inputBaseDir: string) {
  const specsPath = path.join(inputBaseDir, 'specification');

  return await findRecursive(specsPath, filePath => {
    if (path.basename(filePath) !== 'readme.md') {
      return false;
    }

    return filePath
      .split(path.sep)
      .some(parent => parent == 'resource-manager');
  });
}

async function findRecursive(basePath: string, filter: (name: string) => boolean): Promise<string[]> {
  let results: string[] = [];

  for (const subPathName of await readdir(basePath)) {
    const subPath = path.resolve(`${basePath}/${subPathName}`);

    const fileStat = await stat(subPath);
    if (fileStat.isDirectory()) {
      const pathResults = await findRecursive(subPath, filter);
      results = results.concat(pathResults);
      continue;
    }

    if (!fileStat.isFile()) {
      continue;
    }

    if (!filter(subPath)) {
      continue;
    }

    results.push(subPath);
  }

  return results;
}

function executeCmd(cwd: string, cmd: string, args: string[]) : Promise<number> {
  return new Promise((resolve, reject) => {
    console.log(`[${cwd}] executing: ${cmd} ${args.join(' ')}`);

    const child = spawn(cmd, args, {
      cwd: cwd,
      windowsHide: true,
    });

    child.stdout.on('data', data => process.stdout.write(chalk.grey(data.toString())));
    child.stderr.on('data', data => process.stdout.write(chalk.red(data.toString())));
    child.on('error', err => {
      reject(err);
    });
    child.on('exit', code => {
      if (code !== 0) {
        reject(`Exited with code ${code}`);
      }
      resolve(code ? code : 0);
    });
  });
}

function executeSynchronous<T>(asyncFunc: () => Promise<T>) {
  series(
    [asyncFunc],
    (error, _) => {
      if (error) {
        throw error;
      }
    });
}