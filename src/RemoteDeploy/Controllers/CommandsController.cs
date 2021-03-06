﻿using System;
using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace RemoteDeploy.Controllers
{
    [Route("api/commands")]
    [ApiController]
    public class CommandsController : ControllerBase
    {
        [HttpPost]
        public IActionResult Exec([FromForm]string ip, [FromForm]string rootUser, [FromForm]string rootPassword, [FromForm]string command)
        {
            var result = $"{rootUser}@{ip}:{command}\r\n";

            try
            {
                Cmd($"rm -rf /keys/{ip}/sshkey || true");
                Cmd($"mkdir -p /keys/{ip}/sshkey");
                Cmd($"ssh-keygen -t rsa -b 4096 -f /keys/{ip}/sshkey/id_rsa -P ''");
                Cmd($"sshpass -p {rootPassword} ssh-copy-id -o \"StrictHostKeyChecking = no\" -o \"UserKnownHostsFile=/dev/null\" -i /keys/{ip}/sshkey/id_rsa.pub {rootUser}@{ip}");
                result += Cmd($"ssh -q -o \"StrictHostKeyChecking no\" -o \"UserKnownHostsFile=/dev/null\" -i /keys/{ip}/sshkey/id_rsa \"{rootUser}@{ip}\" \"{command}\"");
                Cmd($"ssh -q -o \"StrictHostKeyChecking no\" -o \"UserKnownHostsFile=/dev/null\" -i /keys/{ip}/sshkey/id_rsa \"{rootUser}@{ip}\" \"rm -f .ssh/authorized_keys\"");
            }
            catch (Exception ex)
            {
                result = ex.Message;
            }

            return Ok(result);
        }

        private string Cmd(string cmd)
        {
            var escapedArgs = cmd.Replace("\"", "\\\"");
            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "/bin/bash",
                    Arguments = $"-c \"{escapedArgs}\"",
                    RedirectStandardOutput = true,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                }
            };

            process.Start();
            var result = process.StandardOutput.ReadToEnd();
            process.WaitForExit();

            return result;
        }
    }
}