<img src="https://code.it4i.cz/ADAS/HEAppE/Middleware/-/wikis/uploads/a2477b110aa6b992103972748944854a/HEAppE_seda.png" alt="heappe" height="75" align="left"/>
<img src="https://code.it4i.cz/ADAS/HEAppE/Middleware/-/wikis/uploads/daff3c4fcf40f18878bf0f42a79fa85f/it4i-logo-new.png" alt="it4i" height="70" align="right"/>
<br/><br/><br/>

# HEAppE Middleware
_High-End Application Execution Middleware_

HPC-as-a-Service is a well known term in the area of high performance computing. It enables users to access an HPC infrastructure without a need to buy and manage their own physical servers or data center infrastructure. Through this service small and medium enterprises (SMEs) can take advantage of the technology without an upfront investment in the hardware. This approach further lowers the entry barrier for users and SMEs who are interested in utilizing massive parallel computers but often do not have the necessary level of expertise in this area.

To provide this simple and intuitive access to the supercomputing infrastructure an in-house application framework called HEAppE has been developed. This framework is utilizing a mid-layer principle, in software terminology also known as middleware. Middleware manages and provides information about submitted and running jobs and their data between the client application and the HPC infrastructure. HEAppE is able to submit required computation or simulation on HPC infrastructure, monitor the progress and notify the user should the need arise. It provides necessary functions for job management, monitoring and reporting, user authentication and authorization, file transfer, encryption, and various notification mechanisms.

Major changes in the latest release includes
- multi-platform **.NET Core** version 
- **OpenAPI** REST API 
- **dockerized** deployment and management 
- updated **PBS** and **Slurm** workload manager adapter 
- **SSH Agent** support
- **multiple tasks** within single computational jobs
- **job arrays** support and **job dependency** support
- extremely **long running** job support
- **OpenID** and **OpenStack** authentication support
- various functional and security updates

# References

HEAppE Middleware has already been successfully used in a number of public and commercial projects:

- in **H2020 project LEXIS** as a part of **LEXIS Platform** to provide the platform's job orchestrator access to a number of HPC systems in several HPC centers; https://lexis-project.eu
- in **crisis decision support system Floreon+** for What-If analysis workflow utilizing HPC clusters; https://floreon.eu
- in ESA's **BLENDED project** - Synergetic use of Blockchain and Deep Learning for Space Data to enable the execution of **Blockchain-based computation and access to the IPFS data network**
- in ESA's **Urban Thematic Exploitation Platform (Urban-TEP)** as a middleware enabling sandbox execution of user-defined docker images on the cluster; https://urban-tep.eo.esa.int
- in **H2020 project ExCaPE** as a part of **Drug Discovery Platform** enabling execution of drug discovery scientific pipelines on a supercomputer; http://excape-h2020.eu
- in the area of **molecular diagnostics and personalized medicine** in the scope of the **Moldimed** project as a part of the **Massive Parallel Sequencing Platform** for analysis of NGS data; https://www.imtm.cz/moldimed
- in the area of **bioimageinformatics** as a integral part of **FIJI plugin** providing unified access to HPC clusters for image data processing; http://fiji.sc

## Licence and Contact Information
HEAppE Middleware is licensed under the **GNU General Public License v3.0**. For suport contact us via **support.heappe@it4i.cz**.

## IT4Innovations national supercomputing center
The IT4Innovations national supercomputing center operates three supercomputers: Barbora (826 TFlop/s, installed 2019), Karolina (15,2 PFlop/s, installed 2021) and a special system for AI computation, DGX-2 (2 PFlop/s in AI, installed in 2019). The supercomputers are available to academic community within the Czech Republic and Europe and industrial community worldwide via HEAppE Middleware.

### Barbora
The Barbora cluster consists of 201 compute nodes, totaling 7232 compute cores with 44544 GB RAM, giving over 848 TFLOP/s theoretical peak performance. Nodes are interconnected through a fully non-blocking fat-tree InfiniBand network, and are equipped with Intel Cascade Lake processors. A few nodes are also equipped with NVIDIA Tesla V100-SXM2.

https://docs.it4i.cz/barbora/hardware-overview/

### NVIDIA DGX-2
The DGX-2 is a very powerful computational node, featuring high end x86_64 processors and 16 NVIDIA V100-SXM3 GPUs. The DGX-2 introduces NVIDIA’s new NVSwitch, enabling 300 GB/s chip-to-chip communication at 12 times the speed of PCIe. With NVLink2, it enables 16x NVIDIA V100-SXM3 GPUs in a single system, for a total bandwidth going beyond 14 TB/s. Featuring pair of Xeon 8168 CPUs, 1.5 TB of memory, and 30 TB of NVMe storage, we get a system that consumes 10 kW, weighs 163.29 kg, but offers double precision performance in excess of 130TF.

https://docs.it4i.cz/dgx2/introduction/

### Karolina
TBD - waiting for the official documentation

### Acknowledgement
This work was supported by The Ministry of Education, Youth and Sports from the National Programme of Sustainability (NPS II) project ”IT4Innovations excellence in science - LQ1602” and by the IT4Innovations infrastructure which is supported from the Large Infrastructures for Research, Experimental Development and Innovations project ”IT4Innovations National Supercomputing Center – LM2015070”.

## Middleware Architecture
*HEAppE's* universally designed software architecture enables unified access to different HPC systems through a simple object-oriented client-server interface using standard REST API. Thus providing HPC capabilities to the users but without the necessity to manage the running jobs form the command-line interface of the HPC scheduler directly on the cluster.

<img src="https://code.it4i.cz/ADAS/HEAppE/Middleware/-/wikis/uploads/b369a9145503d97a242466b06c65c223/architecture.png" alt="architecture" width="75%" align="center"/>

## REST API

Main API endpoints
- UserAndLimitationManagement
- ClusterInformation
- JobManagement
- FileTransfer
- DataTransfer
- JobReporting

## Command Template Preparation

For security purposes *HEAppE* enables the users to run only pre-prepared set of so-called *Command Templates*. Each template defines arbitrary script or executable file that will be executed on the cluster, any dependencies or third-party software it might require and the type queue that should be used for the processing (type of computing nodes to be used on the cluster). The template also contains the set of input parameters that will be passed to the executable script during run-time. Thus, the users are only able to execute pre-prepared command templates with the pre-defined set of input parameters. The actual value of each parameter (input from the user) can be changed by the user for each job submission.

| Id | Name | Description | Code | Executable File | Command Parameters | Preparation Script | Cluster Node Type |
|----|------|-------------|------|------|------|------|------|
| 1 | TestTemplate  | Desc | Code | /scratch/temp/Heappe/test.sh | "%%{inputParam}" | module load Python/2.7.9-intel-2015b; | 7 |


## Workflow

1. **UserAndLimitationManagement**<br/>
-> AuthenticateUserPassword - authentication method request<br/>
<- session-code
2. **ClusterInformation**<br/>
-> ListAvailableClusters - get cluster information<br/>
<- General information about cluster, command templates, etc.
3. **JobManagement**<br/>
-> CreateJob - cluster and job specification<br/>
<- job information
4. **FileTransfer**<br/>
-> GetFileTransferMethod - request access to job's storage<br/>
<- FileTransferMethod - information how to access the storage<br/>
-> upload input files<br/>
-> EndFileTransfer - remove storage access<br/>
<- storage access removed
5. **JobManagement**<br/>
-> SubmitJob - submit the job to the cluster's processing queue<br/>
<- job information<br/>
-> GetCurrentInfoForJob - monitor the state of the specified job (queued, running, finished, failed, etc.)<br/>
<- job information
6. **FileTransfer**<br/>
-> GetFileTransferMethod - request access to job's storage<br/>
<- FileTransferMethod - information how to access the storage<br/>
-> download output files<br/>
-> EndFileTransfer - remove storage access<br/>
<- storage access removed
7. **JobReporting**<br/>
-> GetResourceUsageReportForJob - generate job's report<br/>
<- resource usage report

## HEAppE Integration Example (C#)

TODO update to the latest version

## Sample Template Output

```
Authenticating user [testuser]...
        Auth OK (Session GUID: 4a5d7017-1992-45b1-8b07-43cf5d421f50)
Created job ID 71.
Uploading file: someInputFile1.txt
File uploaded.
Uploading file: someInputFile2.txt
File uploaded.
Submitted job ID: 71
Queued
File: StandardErrorFile, console_Stderr
TaskInfoId: 71
Offset: 0
Content:
File: StandardOutputFile, console_Stdout
TaskInfoId: 71
Offset: 0
Content: Input param: someStringParam
Iteration: 01

Running
File: StandardErrorFile, console_Stderr
TaskInfoId: 71
Offset: 0
Content:
File: StandardOutputFile, console_Stdout
TaskInfoId: 71
Offset: 0
Content: Input param: someStringParam
Iteration: 01
Iteration: 02

Finished
File: StandardErrorFile, console_Stderr
TaskInfoId: 71
Offset: 0
Content:
File: StandardOutputFile, console_Stdout
TaskInfoId: 71
Offset: 0
Content: Input param: someStringParam
Iteration: 01
Iteration: 02
Iteration: 03
Iteration: 04
Iteration: 05
Iteration: 06
Iteration: 07
Iteration: 08
Iteration: 09
Iteration: 10

Downloading file: /resultFile.txt
Downloading file: /console_Stdout
Downloading file: /console_Stderr
Press any key to continue . . .
```
