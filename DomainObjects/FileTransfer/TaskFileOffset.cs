﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace HEAppE.DomainObjects.FileTransfer {
	public class TaskFileOffset {
		public long SubmittedTaskInfoId { get; set; }
		public SynchronizableFiles FileType { get; set; }
		public long Offset { get; set; }
	}
}
